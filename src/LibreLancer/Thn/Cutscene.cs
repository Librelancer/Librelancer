// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using LibreLancer.Thorn;
using LibreLancer.Utf.Dfm;

namespace LibreLancer
{
    public class ThnObject
    {
        public string Name;
        public Vector3 Translate;
        public Matrix4x4 Rotate;
        public GameObject Object;
        public DynamicLight Light;
        public ThnEntity Entity;
        public ThnCameraTransform Camera;
        public Vector3 LightDir;
        public Hardpoint HpMount;
        public ThnSound Sound;
        public bool Animating = false;
        public void Update()
        {
            if (Object != null)
            {
                if (Object.RenderComponent is CharacterRenderer charRen)
                {
                    if (charRen.Skeleton.ApplyRootMotion)
                    {
                        var newTranslate = charRen.Skeleton.RootTranslation - charRen.Skeleton.RootTranslationOrigin;
                        var newRotate = charRen.Skeleton.RootRotation * charRen.Skeleton.RootRotationOrigin;
                        charRen.Skeleton.RootRotationOrigin = Quaternion.Inverse(charRen.Skeleton.RootRotation);
                        charRen.Skeleton.RootTranslationOrigin = charRen.Skeleton.RootTranslation;
                        Rotate = Matrix4x4.CreateFromQuaternion(newRotate) * Rotate;
                        Translate += Vector3.Transform(newTranslate, newRotate);
                    }
                    Translate.Y = charRen.Skeleton.FloorHeight + charRen.Skeleton.RootHeight;
                }
                if(HpMount == null)
                    Object.Transform = Rotate * Matrix4x4.CreateTranslation(Translate);
                else {
                    var tr = HpMount.Transform;
                    Matrix4x4.Invert(tr, out tr);
                    Object.Transform = tr * (Rotate * Matrix4x4.CreateTranslation(Translate));
                }
            }
            if(Camera != null)
            {
                Camera.Orientation = Rotate;
                Camera.Position = Translate;
            }
            if(Light != null)
            {
                Light.Light.Position = Translate;
                Light.Light.Direction = Vector3.TransformNormal(LightDir.Normalized(), Rotate);
            }
        }
    }

    public interface IThnRoutine
    {
        bool Run(Cutscene cs, double delta);
    }

    //TODO: PCurves
    public class Cutscene : IDisposable
	{
		double currentTime = 0;
        double totalDuration;
		Queue<ThnEvent> events = new Queue<ThnEvent>();
		public Dictionary<string, ThnObject> Objects = new Dictionary<string, ThnObject>(StringComparer.OrdinalIgnoreCase);
		public List<IThnRoutine> Coroutines = new List<IThnRoutine>();
		//ThnScript thn;

		public GameWorld World;
		public SystemRenderer Renderer;

		ThnCamera camera;
        ThnScriptContext scriptContext;
        public ICamera CameraHandle => camera;
        bool spawnObjects = true;
        public bool Running => currentTime < totalDuration;

        //Event processing
        static Dictionary<EventTypes, IThnEventRunner> eventRunners = new Dictionary<EventTypes, IThnEventRunner>();
        static Cutscene()
        {
            foreach(var type in typeof(Cutscene).Assembly.GetTypes())
            {
                foreach(var attr in type.CustomAttributes)
                {
                    if(attr.AttributeType == typeof(ThnEventRunnerAttribute))
                    {
                        eventRunners.Add((EventTypes)attr.ConstructorArguments[0].Value, (IThnEventRunner)Activator.CreateInstance(type));
                        break;
                    }
                }
            }
        }

        void AddEntities(ThnScript thn)
        {
            foreach (var kv in thn.Entities)
            {
                if (Objects.ContainsKey(kv.Key)) continue;
                if ((kv.Value.ObjectFlags & ThnObjectFlags.Reference) == ThnObjectFlags.Reference) continue;
                var obj = new ThnObject();
                obj.Name = kv.Key;
                obj.Translate = kv.Value.Position ?? Vector3.Zero;
                obj.Rotate = kv.Value.RotationMatrix ?? Matrix4x4.Identity;
                //PlayerShip object
                if (spawnObjects  && scriptContext.PlayerShip != null && kv.Value.Type == EntityTypes.Compound &&
                kv.Value.Template.Equals("playership", StringComparison.InvariantCultureIgnoreCase))
                {
                    obj.Object = scriptContext.PlayerShip;
                    obj.Object.RenderComponent.LitDynamic = (kv.Value.ObjectFlags & ThnObjectFlags.LitDynamic) == ThnObjectFlags.LitDynamic;
                    obj.Object.RenderComponent.LitAmbient = (kv.Value.ObjectFlags & ThnObjectFlags.LitAmbient) == ThnObjectFlags.LitAmbient;
                    obj.Object.RenderComponent.NoFog = kv.Value.NoFog;
                    ((ModelRenderer)obj.Object.RenderComponent).LightGroup = kv.Value.LightGroup;
                    obj.Entity = kv.Value;
                    Vector3 transform = kv.Value.Position ?? Vector3.Zero;
                    obj.Object.Transform = (kv.Value.RotationMatrix ?? Matrix4x4.Identity) * Matrix4x4.CreateTranslation(transform);
                    obj.HpMount = scriptContext.PlayerShip.GetHardpoint("HpMount");
                    World.Objects.Add(obj.Object);
                    Objects.Add(kv.Key, obj);
                    continue;
                }

                var template = kv.Value.Template;
                string replacement;
                if (scriptContext != null &&
                    scriptContext.Substitutions.TryGetValue(kv.Value.Template, out replacement))
                    template = replacement;
                var resman = game.GetService<ResourceManager>();
                if (spawnObjects && kv.Value.Type == EntityTypes.Compound)
                {
                    bool getHpMount = false;
                    //Fetch model
                    IDrawable drawable;
                    switch (kv.Value.MeshCategory.ToLowerInvariant())
                    {
                        case "solar":
                            drawable = gameData.GetSolar(template);
                            break;
                        case "ship":
                        case "spaceship":
                            getHpMount = true;
                            var sh = gameData.GetShip(template);
                            drawable = sh.ModelFile.LoadFile(resman);
                            break;
                        case "prop":
                            drawable = gameData.GetProp(template);
                            break;
                        case "room":
                            drawable = gameData.GetRoom(template);
                            break;
                        case "equipment cart":
                            drawable = gameData.GetCart(template);
                            break;
                        case "equipment":
                            var eq = gameData.GetEquipment(template);
                            drawable = eq.ModelFile.LoadFile(resman);
                            break;
                        case "asteroid":
                            drawable = gameData.GetAsteroid(kv.Value.Template);
                            break;
                        default:
                            throw new NotImplementedException("Mesh Category " + kv.Value.MeshCategory);
                    }
                    drawable?.Initialize(resman);
                    if (kv.Value.UserFlag != 0)  {
                        //This is a starsphere
                        layers.Add(new Tuple<IDrawable, ThnObject>(drawable, obj));
                    }
                    else
                    {
                        obj.Object = new GameObject(drawable, game.GetService<ResourceManager>(), true, false);
                        obj.Object.Name = kv.Value.Name;
                        obj.Object.PhysicsComponent = null; //Jitter seems to interfere with directly setting orientation
                        if (getHpMount)
                            obj.HpMount = obj.Object.GetHardpoint("HpMount");
                        var r = (ModelRenderer)obj.Object.RenderComponent;
                        if (r != null)
                        {
                            r.LightGroup = kv.Value.LightGroup;
                            r.LitDynamic = (kv.Value.ObjectFlags & ThnObjectFlags.LitDynamic) ==
                                           ThnObjectFlags.LitDynamic;
                            r.LitAmbient = (kv.Value.ObjectFlags & ThnObjectFlags.LitAmbient) ==
                                           ThnObjectFlags.LitAmbient;
                            //HIDDEN just seems to be an editor flag?
                            //r.Hidden = (kv.Value.ObjectFlags & ThnObjectFlags.Hidden) == ThnObjectFlags.Hidden;
                            r.NoFog = kv.Value.NoFog;
                        }
                    }
                }
                else if (kv.Value.Type == EntityTypes.PSys)
                {
                    var fx = gameData.GetEffect(kv.Value.Template);
                    obj.Object = new GameObject();
                    obj.Object.RenderComponent = new ParticleEffectRenderer(fx.GetEffect(resman)) { Active = false };
                }
                else if (kv.Value.Type == EntityTypes.Scene)
                {
                    if (hasScene)
                    {
                        //throw new Exception("Thn can only have one scene");
                        //TODO: This needs to be handled better
                        continue;
                    }
                    var amb = kv.Value.Ambient.Value;
                    if (amb.X == 0 && amb.Y == 0 && amb.Z == 0) continue;
                    hasScene = true;
                    Renderer.SystemLighting.Ambient = new Color4(amb.X / 255f, amb.Y / 255f, amb.Z / 255f, 1);
                }
                else if (kv.Value.Type == EntityTypes.Light)
                {
                    var lt = new DynamicLight();
                    lt.LightGroup = kv.Value.LightGroup;
                    lt.Active = kv.Value.LightProps.On;
                    lt.Light = kv.Value.LightProps.Render;
                    obj.Light = lt;
                    obj.LightDir = lt.Light.Direction;
                    if (kv.Value.RotationMatrix.HasValue)
                    {
                        var m = kv.Value.RotationMatrix.Value;
                        lt.Light.Direction = Vector3.TransformNormal(lt.Light.Direction, m);
                    }
                    if(Renderer != null)
                        Renderer.SystemLighting.Lights.Add(lt);
                }
                else if (kv.Value.Type == EntityTypes.Camera)
                {
                    obj.Camera = new ThnCameraTransform();
                    obj.Camera.Position = kv.Value.Position.Value;
                    obj.Camera.Orientation = kv.Value.RotationMatrix ?? Matrix4x4.Identity;
                    obj.Camera.FovH = kv.Value.FovH ?? obj.Camera.FovH;
                    obj.Camera.AspectRatio = kv.Value.HVAspect ?? obj.Camera.AspectRatio;
                    if (kv.Value.NearPlane != null) obj.Camera.Znear = kv.Value.NearPlane.Value;
                    if (kv.Value.FarPlane != null) obj.Camera.Zfar = kv.Value.FarPlane.Value;
                }
                else if (kv.Value.Type == EntityTypes.Marker)
                {
                    obj.Object = new GameObject();
                    obj.Object.Name = "Marker";
                    obj.Object.Nickname = "";
                }
                else if (kv.Value.Type == EntityTypes.Deformable)
                {
                    obj.Object = new GameObject();
                    gameData.GetCostume(template, out DfmFile body, out DfmFile head, out DfmFile leftHand, out DfmFile rightHand);
                    var skel = new DfmSkeletonManager(body, head, leftHand, rightHand);
                    obj.Object.RenderComponent = new CharacterRenderer(skel);
                    var anmComponent = new AnimationComponent(obj.Object, gameData.GetCharacterAnimations());
                    obj.Object.AnimationComponent = anmComponent;
                    obj.Object.Components.Add(anmComponent);
                }
                else if (kv.Value.Type == EntityTypes.Sound)
                {
                    obj.Sound = new ThnSound(kv.Value.Template, game.GetService<SoundManager>(), kv.Value.AudioProps, obj);
                    obj.Sound.Spatial = (kv.Value.ObjectFlags & ThnObjectFlags.Spatial) == ThnObjectFlags.Spatial;
                }
                if (obj.Object != null)
                {
                    Vector3 transform = kv.Value.Position ?? Vector3.Zero;
                    obj.Object.Transform = (kv.Value.RotationMatrix ?? Matrix4x4.Identity) * Matrix4x4.CreateTranslation(transform);
                    World.Objects.Add(obj.Object);
                }
                obj.Entity = kv.Value;
                Objects[kv.Key] = obj;
            }
        }

        bool hasScene = false;
        Game game;
        private GameDataManager gameData;
        List<Tuple<IDrawable, ThnObject>> layers = new List<Tuple<IDrawable, ThnObject>>();

        public Cutscene(IEnumerable<ThnScript> scripts, SpaceGameplay gameplay)
        {
            this.game = gameplay.FlGame;
            this.gameData = gameplay.FlGame.GameData;
            World = gameplay.world;
            spawnObjects = false;
            camera = new ThnCamera(gameplay.FlGame.Viewport);
            //thn = script;
            var evs = new List<ThnEvent>();
            foreach (var thn in scripts)
            {
                totalDuration = Math.Max(totalDuration, thn.Duration);
                foreach (var ev in thn.Events)
                {
                    ev.TimeOffset = 0;
                    evs.Add(ev);
                }
                AddEntities(thn);
            }
            evs.Sort((x, y) => x.Time.CompareTo(y.Time));
            foreach (var item in evs)
                events.Enqueue(item);
        }

        public Cutscene(ThnScriptContext context, GameDataManager gameData, Viewport viewport, Game game)
		{
            this.game = game;
            this.gameData = gameData;
            scriptContext = context;
			camera = new ThnCamera(viewport);

			Renderer = new SystemRenderer(camera, gameData, game.GetService<GameResourceManager>(), game);
			World = new GameWorld(Renderer);
			//thn = script;
			var evs = new List<ThnEvent>();
			foreach (var thn in context.Scripts)
			{
                totalDuration = Math.Max(totalDuration, thn.Duration);
                foreach (var ev in thn.Events) {
                    ev.TimeOffset = 0;
                    evs.Add(ev);
                }
                AddEntities(thn);
			}
            //work around SET_CAMERA not being called in disco (match vanilla behaviour)
            var firstCamera = Objects.Values.Where(x => x.Camera != null).FirstOrDefault();
            if(firstCamera != null) {
                camera.Transform = firstCamera.Camera;
            }
            evs.Sort((x, y) => x.Time.CompareTo(y.Time));
			foreach (var item in evs)
				events.Enqueue(item);
            //Add starspheres in the right order
            var sorted = ((IEnumerable<Tuple<IDrawable, ThnObject>>)layers).Reverse().OrderBy(x => x.Item2.Entity.SortGroup).ToArray();
			Renderer.StarSphereModels = new RigidModel[sorted.Length];
			Renderer.StarSphereWorlds = new Matrix4x4[sorted.Length];
            Renderer.StarSphereLightings = new Lighting[sorted.Length];
            starSphereObjects = new ThnObject[sorted.Length];
			for (int i = 0; i < sorted.Length; i++)
			{
				Renderer.StarSphereModels[i] = (sorted[i].Item1 as IRigidModelFile).CreateRigidModel(true);
                Renderer.StarSphereWorlds[i] = sorted[i].Item2.Rotate * Matrix4x4.CreateTranslation(sorted[i].Item2.Translate);
                Renderer.StarSphereLightings[i] = Lighting.Empty;
                starSphereObjects[i] = sorted[i].Item2;
            }
			//Add objects to the renderer
			World.RegisterAll();
		}

        private ThnObject[] starSphereObjects;

        void UpdateStarsphere()
        {
            for (int i = 0; i < starSphereObjects.Length; i++)
            {
                Renderer.StarSphereWorlds[i] = starSphereObjects[i].Rotate * Matrix4x4.CreateTranslation(starSphereObjects[i].Translate);
                var ldynamic = (starSphereObjects[i].Entity.ObjectFlags & ThnObjectFlags.LitDynamic) == ThnObjectFlags.LitDynamic;
                var lambient = (starSphereObjects[i].Entity.ObjectFlags & ThnObjectFlags.LitAmbient) == ThnObjectFlags.LitAmbient;
                var nofog = starSphereObjects[i].Entity.NoFog;
                Renderer.StarSphereLightings[i] = RenderHelpers.ApplyLights(Renderer.SystemLighting,
                    starSphereObjects[i].Entity.LightGroup, Vector3.Zero, float.MaxValue, null,
                    lambient, ldynamic, nofog);
            }
        }
        
        public void RunScript(ThnScript thn, Action onFinish = null)
        {
            AddEntities(thn);
            var evArr = events.ToArray();
            var evsNew = new List<ThnEvent>(evArr);
            foreach(var ev in thn.Events) {
                totalDuration = Math.Max(totalDuration, thn.Duration);
                ev.TimeOffset = currentTime;
                evsNew.Add(ev);
            }
            if (onFinish != null)
            {
                evsNew.Add(new ThnEvent()
                {
                    TimeOffset = thn.Duration,
                    CustomAction = onFinish
                });
            }
            evsNew.Sort((x, y) => x.Time.CompareTo(y.Time));
            events = new Queue<ThnEvent>();
            foreach (var item in evsNew)
                events.Enqueue(item);
            
        }

        double accumTime = 0;
		const int MAX_STEPS = 8;
		const double TIMESTEP = 1.0 / 120.0;
		public void Update(TimeSpan delta)
		{
			int counter = 0;
			accumTime += delta.TotalSeconds;

			while (accumTime > (1.0 / 120.0))
			{
				_Update(TimeSpan.FromSeconds(TIMESTEP));

				accumTime -= TIMESTEP;
				counter++;

				if (counter > MAX_STEPS)
				{
					// okay, okay... we can't keep up
					FLLog.Warning("Thn", "Can't keep up!");
					accumTime = 0.0f;
					break;
				}
			}
        }
		public void _Update(TimeSpan delta)
        {
            var sound = game.GetService<SoundManager>();
            if (Running)
            {
                var pos = camera.Transform.Position;
                var forward = Vector3.TransformNormal(-Vector3.UnitZ, camera.Transform.Orientation);
                var up = Vector3.TransformNormal(Vector3.UnitY, camera.Transform.Orientation);
                sound.UpdateListener(delta, pos, forward, up);
            }
			currentTime += delta.TotalSeconds;
			for (int i = (Coroutines.Count - 1); i >= 0; i--)
			{
				if (!Coroutines[i].Run(this, delta.TotalSeconds))
				{
					Coroutines.RemoveAt(i);
					i--;
				}
			}
			while (events.Count > 0 && events.Peek().Time <= currentTime)
			{
				var ev = events.Dequeue();
				ProcessEvent(ev);
			}
            foreach (var obj in Objects.Values) obj.Update();
			camera.Update();
            if(Renderer != null)
			    World.Update(delta);
		}

		public void Draw()
        {
            UpdateStarsphere();
			Renderer.Draw();
		}

		void ProcessEvent(ThnEvent ev)
		{
            if (ev.CustomAction != null)
            {
                ev.CustomAction();
                return;
            }
            if(ev.Type == EventTypes.SetCamera)
                ProcessSetCamera(ev);
            else if (ev.Type == EventTypes.StartPSys)
                ProcessStartPSys(ev);
            else
            {
                IThnEventRunner er;
                if(eventRunners.TryGetValue(ev.Type, out er))
                    er.Process(ev, this);
                else
                    FLLog.Error("Thn", "Unimplemented event: " + ev.Type.ToString());
            }
        }

  		public void SetCamera(string name)
		{
			var cam = Objects[name];
			camera.Transform = cam.Camera;
            var sound = game.GetService<SoundManager>();
            sound.ResetListenerVelocity();
        }
        void ProcessSetCamera(ThnEvent ev)
		{
			SetCamera((string)ev.Targets[1]);
		}

		void ProcessStartPSys(ThnEvent ev)
		{
            if (!Objects.ContainsKey((string) ev.Targets[0]))
            {
                FLLog.Error("Thn", "Entity " + ev.Targets[0].ToString()+ " does not exist");
                return;
            }
			var obj = Objects[(string)ev.Targets[0]];
            var r = (ParticleEffectRenderer)obj.Object.RenderComponent;
            r.Active = true;
            Coroutines.Add(new StopPSys() { Duration = ev.Duration, Fx = r });
		}

        class StopPSys : IThnRoutine
        {
            double time;
            public double Duration;
            public ParticleEffectRenderer Fx;
            public bool Run(Cutscene cs, double delta)
            {
                time += delta;
                if(time >= Duration)
                {
                    Fx.Active = false;
                    return false;
                }
                return true;
            }
        }
        public void Dispose()
		{
			Renderer.Dispose();
		}
	}
}

