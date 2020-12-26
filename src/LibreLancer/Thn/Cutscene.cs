// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

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
                        Translate += Vector3.Transform(newTranslate, Rotate);
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
    public class Cutscene : IDisposable
	{
        public const float LETTERBOX_HEIGHT = 0.138021f;
        //Public variables
		public GameWorld World;
		public SystemRenderer Renderer;
        public GameObject PlayerShip => scriptContext.PlayerShip;
        //Public properties
        public Game Game => game;
        public ICamera CameraHandle => camera;
        public Dictionary<string, string> Substitutions => scriptContext.Substitutions;
        public GameDataManager GameData => gameData;
        public bool Running => running;

        //Private variables
        double currentTime = 0;
        private Dictionary<string, ThnObject> sceneObjects;
        Game game;
        ThnCamera camera;
        bool spawnObjects = true;
        List<Tuple<IDrawable, ThnObject>> layers = new List<Tuple<IDrawable, ThnObject>>();
        ThnDisplayText text;
        private GameDataManager gameData;
        bool hasScene = false;
        private bool running = false;

        public event Action<ThnScript> ScriptFinished;

        public void OnScriptFinished(ThnScript thn) => ScriptFinished?.Invoke(thn);

        public void AddStarsphere(IDrawable drawable, ThnObject obj)
        {
            layers.Add(new Tuple<IDrawable, ThnObject>(drawable, obj));
        }

        public void SetDisplayText(ThnDisplayText text)
        {
            this.text = text;
        }

        public void SetAmbient(Vector3 amb)
        {
            if (hasScene) return;
            Renderer.SystemLighting.Ambient = new Color4(amb.X / 255f, amb.Y / 255f, amb.Z / 255f, 1);
            hasScene = true;
        }

        public Cutscene(ThnScriptContext context, SpaceGameplay gameplay)
        {
            game = gameplay.FlGame;
            gameData = gameplay.FlGame.GameData;
            World = gameplay.world;
            spawnObjects = false;
            camera = new ThnCamera(gameplay.FlGame.Viewport);
            scriptContext = context;
        }

        private ThnScriptContext scriptContext;
        List<ThnScriptInstance> instances = new List<ThnScriptInstance>();
        public Cutscene(ThnScriptContext context, GameDataManager gameData, Viewport viewport, Game game)
        {
            scriptContext = context;
            this.game = game;
            this.gameData = gameData;
			camera = new ThnCamera(viewport);
        }

        public void BeginScene(params ThnScript[] scene) => BeginScene((IEnumerable<ThnScript>)scene);
        public void BeginScene(IEnumerable<ThnScript> scene)
        {
            var scripts = scene.ToArray();
            SceneSetup(scripts);
        }
        void SceneSetup(ThnScript[] scripts)
        {
            hasScene = false;
            currentTime = 0;
            sceneObjects = new Dictionary<string, ThnObject>(StringComparer.OrdinalIgnoreCase);
            layers = new List<Tuple<IDrawable, ThnObject>>();
            if (spawnObjects) {
                if (Renderer != null)
                {
                    Renderer.Dispose();
                    World.Dispose();
                }
                Renderer = new SystemRenderer(camera, gameData, game.GetService<GameResourceManager>(), game);
                World = new GameWorld(Renderer, false);
            }
            if (scriptContext.SetScript != null)
            {
                var inst = new ThnScriptInstance(this, scriptContext.SetScript);
                inst.ConstructEntities(sceneObjects, spawnObjects);
            }
            if (instances != null)
            {
                foreach (var inst in instances)
                    inst.Cleanup();
            }
            instances = new List<ThnScriptInstance>();
            foreach (var script in scripts)
            {
                var ts = new ThnScriptInstance(this, script);
                ts.ConstructEntities(sceneObjects, spawnObjects);
                instances.Add(ts);
            }
            var firstCamera = sceneObjects.Values.FirstOrDefault(x => x.Camera != null);
            if (firstCamera == null) firstCamera = sceneObjects.Values.FirstOrDefault(x => x.Camera != null);
            if(firstCamera != null) {
                camera.Transform = firstCamera.Camera;
            }

            if (spawnObjects)
            {
                //Add starspheres in the right order
                var sorted = ((IEnumerable<Tuple<IDrawable, ThnObject>>) layers).Reverse()
                    .OrderBy(x => x.Item2.Entity.SortGroup).ToArray();
                Renderer.StarSphereModels = new RigidModel[sorted.Length];
                Renderer.StarSphereWorlds = new Matrix4x4[sorted.Length];
                Renderer.StarSphereLightings = new Lighting[sorted.Length];
                starSphereObjects = new ThnObject[sorted.Length];
                for (int i = 0; i < sorted.Length; i++)
                {
                    Renderer.StarSphereModels[i] = (sorted[i].Item1 as IRigidModelFile).CreateRigidModel(true);
                    Renderer.StarSphereWorlds[i] =
                        sorted[i].Item2.Rotate * Matrix4x4.CreateTranslation(sorted[i].Item2.Translate);
                    Renderer.StarSphereLightings[i] = Lighting.Empty;
                    starSphereObjects[i] = sorted[i].Item2;
                }
                //Add objects to the renderer
                World.RegisterAll();
            }

            lagCounter = 0;
            //Init
            _Update(TimeSpan.Zero);
            running = true;
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

        private const double TIMESTEP = 1.0 / 120.0;
        private double accumTime = 0;
        private int lagCounter = 0;
        private int LAG_LIMIT = 5;
        private const double LAG_THRESHOLD = 1 / 20.0;
		public void Update(TimeSpan delta)
        {
            if (lagCounter < LAG_LIMIT && delta.TotalSeconds > LAG_THRESHOLD)
            {
                lagCounter++;
                return;
            }
            accumTime += delta.TotalSeconds;
            if (accumTime >= TIMESTEP)
            {
                _Update(TimeSpan.FromSeconds(accumTime));
                accumTime = 0;
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
            if (text != null)
            {
                if (currentTime > text.Start)
                {
                    game.GetService<Interface.Typewriter>().PlayString(gameData.GetString(text.TextIDS));
                    text = null;
                }
            }
            //
            foreach (var instance in instances)
            {
                instance.Update(delta);
            }
            //
            foreach (var obj in sceneObjects.Values) obj.Update();
			camera.Update();
            if(Renderer != null)
			    World.Update(delta);
		}

		public void Draw()
        {
            UpdateStarsphere();
			Renderer.Draw();
        }

        public ThnObject GetObject(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            ThnObject o;
            if (sceneObjects.TryGetValue(name, out o))
                return o;
            return null;
        }
        public void SetCamera(string name)
        {
            var cam = GetObject(name);
            camera.Object = cam;
			camera.Transform = cam.Camera;
            var sound = game.GetService<SoundManager>();
            sound.ResetListenerVelocity();
        }
        public void Dispose()
		{
			Renderer.Dispose();
		}
	}
}