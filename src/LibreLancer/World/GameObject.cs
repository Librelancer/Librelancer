// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.Client.Components;
using LibreLancer.GameData;
using LibreLancer.GameData.Items;
using LibreLancer.GameData.World;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Utf.Mat;
using LibreLancer.World.Components;
using Archs = LibreLancer.GameData.Archetypes;

namespace LibreLancer.World
{
    public enum GameObjectKind
    {
        None,
        Ship,
        Solar,
        Missile
    }

    public class TradelaneName : ObjectName
    {
        private GameObject Parent;
        public TradelaneName(GameObject parent, params int[] ids)
        {
            Parent = parent;
            this._Ids = ids;
        }

        private string leftRight = "";
        private string rightLeft = "";
        public override string GetName(GameDataManager gameData, Vector3 other)
        {
            var heading = other - Parent.PhysicsComponent.Body.Position;
            var fwd = Parent.PhysicsComponent.Body.Transform.GetForward();
            var dot = Vector3.Dot(heading, fwd);
            if (string.IsNullOrEmpty(leftRight) ||
                string.IsNullOrEmpty(rightLeft))
            {
                leftRight = $"{gameData.GetString(_Ids[0])}->{gameData.GetString(_Ids[1])}";
                rightLeft = $"{gameData.GetString(_Ids[1])}->{gameData.GetString(_Ids[0])}";
            }
            if (dot > 0)
            {
                return leftRight;
            }
            else
            {
                return rightLeft;
            }
        }
    }

    public class ObjectName
    {
        internal string _NameString = null;
        internal int[] _Ids = null;

        private bool dirty = true;
        private string cached;

        public ObjectName(params int[] ids)
        {
            this._Ids = ids;
        }

        public ObjectName(string str)
        {
            this._NameString = str;
        }

        public virtual string GetName(GameDataManager gameData, Vector3 other)
        {
            if (dirty)
            {
                if (_Ids != null)
                    cached = string.Join(' ', _Ids.Select(gameData.GetString));
                else
                    cached = _NameString;
                dirty = false;
            }
            return cached;
        }


        public override string ToString()
        {
            if (!string.IsNullOrEmpty(cached)) return cached;
            else if (_Ids != null)
            {
                return "IDS: " + string.Join(',', _Ids.Select(x => x.ToString()));
            } else if (_NameString != null)
                return _NameString;
            else
                return "(NULL)";
        }
    }

    [Flags]
    public enum GameObjectFlags
    {
        Exists = 1 << 0,
        Important = 1 << 1,
        Neutral = 1 << 2,
        Friendly = 1 << 3,
        Hostile = 1 << 4,
        Reputations = Neutral | Friendly | Hostile,
    }


	public class GameObject
	{
        //
        private static int _unique = 0;

        public readonly int Unique = Interlocked.Increment(ref _unique);
		//Object data
		public ObjectName Name;

        private string _nickname;
        public uint NicknameCRC { get; private set; }
        public string Nickname
        {
            get
            {
                return _nickname;
            }
            set
            {
                _nickname = value;
                if (value == null)
                    NicknameCRC = 0;
                else
                    NicknameCRC = CrcTool.FLModelCrc(value);
            }
        }

        public GameObjectFlags Flags;
        public ShipFormation Formation = null;
        public static object ClientPlayerTag = new object();
        public GameObjectKind Kind = GameObjectKind.None;
        public object Tag;
        public string ArchetypeName;
        public int NetID;
		public Hardpoint _attachment;
		Matrix4x4 _localTransform = Matrix4x4.Identity;
		public Matrix4x4 LocalTransform
		{
			get
			{
                return _localTransform;
			}
		}

        public void SetLocalTransform(Matrix4x4 tr, bool phys = false)
        {
            _localTransform = tr;
            transformDirty = true;
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].transformDirty = true;
            }
            if (!phys && PhysicsComponent != null && PhysicsComponent.Body != null)
            {
                PhysicsComponent.Body.SetTransform(tr);
            }
        }

        public void ForceTransformDirty() => transformDirty = true;

        Matrix4x4 CalculateTransform()
        {
            var tr = LocalTransform;
            if (_attachment != null)
                tr *= _attachment.Transform;
            if (_parent != null)
                tr *= _parent.WorldTransform;
            return tr;
        }

        private bool transformDirty = false;
        Matrix4x4 worldTransform = Matrix4x4.Identity;

        public Matrix4x4 WorldTransform
        {
            get {
                if (transformDirty)
                {
                    transformDirty = false;
                    worldTransform = CalculateTransform();
                }
                return worldTransform;
            }
        }


        private GameObject _parent;
		IDrawable dr;
		Dictionary<string, Hardpoint> hardpoints = new Dictionary<string, Hardpoint>(StringComparer.OrdinalIgnoreCase);
		//Components
		public List<GameObject> Children = new List<GameObject>();
		public List<GameComponent> Components = new List<GameComponent>();
        public Data.Solar.CollisionGroup[] CollisionGroups;
		public ObjectRenderer RenderComponent;
		public PhysicsComponent PhysicsComponent;
		public AnimationComponent AnimationComponent;
		public SystemObject SystemObject;
        public RigidModel RigidModel;

        public GameObject Parent
        {
            get => _parent;
            set {
                _parent = value;
                transformDirty = true;
            }
        }

        public Hardpoint Attachment
        {
            get => _attachment;
            set {
                _attachment = value;
                transformDirty = true;
            }
        }

        public GameObject(Archetype arch, ResourceManager res, bool draw = true, bool phys = true)
        {
            Kind = GameObjectKind.Solar;
			if (arch is Archs.Sun)
			{
				RenderComponent = new SunRenderer((Archs.Sun)arch);
            }
			else
			{
				InitWithDrawable(arch.ModelFile.LoadFile(res), res, draw, phys);
			}
		}
		public GameObject()
		{

		}
        public static GameObject WithModel(ResolvedModel modelFile, bool draw, ResourceManager res)
        {
            var go = new GameObject();
            go.InitWithDrawable(modelFile.LoadFile(res), res, draw,  false);
            return go;
        }
		public GameObject(IDrawable drawable, ResourceManager res, bool draw = true,  bool phys = true)
		{
            InitWithDrawable(drawable, res, draw,  phys);
        }
        public GameObject(Ship ship, ResourceManager res, bool draw = true, bool phys = false)
        {
            InitWithDrawable(ship.ModelFile.LoadFile(res), res, draw, phys);
            ArchetypeName = ship.Nickname;
            Kind = GameObjectKind.Ship;
            if (RenderComponent != null)
            {
                RenderComponent.LODRanges = ship.LODRanges;
            }
            if (PhysicsComponent != null)
            {
                PhysicsComponent.Mass = ship.Mass;
                PhysicsComponent.Inertia = ship.RotationInertia;
            }
        }

        public GameObject(string name, RigidModel model, ResourceManager res, string partName, float mass, bool draw)
        {
            RigidModel = model;
            Resources = res;
            PopulateHardpoints();
            if (draw && RigidModel != null)
            {
                RenderComponent = new ModelRenderer(RigidModel) { Name = name };
            }
            var path = Path.ChangeExtension(RigidModel.Path, "sur");
            name = Path.GetFileNameWithoutExtension(RigidModel.Path);
            uint plainCrc = 0;
            if (!string.IsNullOrEmpty(partName)) plainCrc = CrcTool.FLModelCrc(partName);
            if (File.Exists(path))
            {
                PhysicsComponent = new PhysicsComponent(this)
                {
                    Sur = res.GetSur(path),
                    Mass = mass,
                    PlainCrc = plainCrc
                };
                Components.Add(PhysicsComponent);
            }
        }
		public void UpdateCollision()
        {
            for (int i = 0; i < Children.Count; i++) Children[i].transformDirty = true;
			if (PhysicsComponent == null) return;
            PhysicsComponent.UpdateParts();
        }

        public void DisableCmpPart(string part)
        {
            if(RigidModel != null && RigidModel.Parts.TryGetValue(part, out var p))
            {
                p.Active = false;
                PhysicsComponent.DisablePart(p);
                World?.Server?.PartDisabled(this, part);
                for (int i = Children.Count - 1; i >= 0; i--)
                {
                    var child = Children[i];
                    if (!(child.Attachment?.Parent?.Active ?? true))
                    {
                        Children.RemoveAt(i);
                    }
                }
            }
        }

        public void SpawnDebris(string part)
        {
            if (World?.Server == null) {
                throw new Exception("Server-only code");
            }
            if (RigidModel != null && RigidModel.Parts.TryGetValue(part, out var srcpart))
            {
                DisableCmpPart(part);
                var tr = srcpart.LocalTransform * WorldTransform;
                var pos0 = Vector3.Transform(Vector3.Zero, WorldTransform);
                var pos1 = Vector3.Transform(Vector3.Zero, tr);
                var vec = (pos1 - pos0).Normalized();
                var initialforce = 100f;
                var mass = 50f;
                if (CollisionGroups != null)
                {
                    var cg = CollisionGroups.FirstOrDefault(x =>
                        x.obj.Equals(part, StringComparison.OrdinalIgnoreCase));
                    if (cg != null)
                    {
                        mass = cg.Mass;
                        initialforce = cg.ChildImpulse;
                    }
                }
                World.Server.SpawnDebris(Kind, ArchetypeName, part, tr, mass, vec * initialforce);
            }
        }
        public ResourceManager Resources;
        void InitWithDrawable(IDrawable drawable, ResourceManager res, bool draw, bool havePhys = true)
		{
			Resources = res;
			dr = drawable;
            PhysicsComponent phys = null;
			bool isCmp = false;
            string name = "";
			if (dr is SphFile)
			{
				var radius = ((SphFile)dr).Radius;
                phys = new PhysicsComponent(this) { SphereRadius = radius };
                name = ((SphFile)dr).SideMaterialNames[0];
                RigidModel = ((SphFile) dr).CreateRigidModel(draw, res);
            }
			else if (dr is IRigidModelFile mdl)
			{
				//var mdl = dr as ModelFile;
                RigidModel = mdl.CreateRigidModel(draw, res);
				var path = Path.ChangeExtension(RigidModel.Path, "sur");
                name = Path.GetFileNameWithoutExtension(RigidModel.Path);
                if (File.Exists(path))
                    phys = new PhysicsComponent(this) { Sur = res.GetSur(path) };
                else if (havePhys)
                {
                    FLLog.Error("Sur", $"Could not load sur file {path}");
                }
                if (RigidModel.Animation != null)
                {
                    AnimationComponent = new AnimationComponent(this, RigidModel.Animation);
                    Components.Add(AnimationComponent);
                }
			}
            if (havePhys && phys != null)
            {
                PhysicsComponent = phys;
                Components.Add(phys);
            }
            PopulateHardpoints();
            if (draw && RigidModel != null)
            {
                RenderComponent = new ModelRenderer(RigidModel) { Name = name };
            }
		}

        public void SetLoadout(ObjectLoadout loadout, bool cutscene = false)
        {
            foreach (var item in loadout.Items)
            {
                var type = cutscene ? EquipmentType.Cutscene :
                    (RenderComponent != null) ? EquipmentType.RemoteObject : EquipmentType.Server;
                if (item.Equipment is AnimationEquipment anm)
                {
                    if(anm.Animation != null)
                        AnimationComponent?.StartAnimation(anm.Animation);
                }
                else
                {
                    EquipmentObjectManager.InstantiateEquipment(this, Resources, null,
                        type, item.Hardpoint ?? "internal", item.Equipment);
                }
            }
        }


		void PopulateHardpoints()
        {
            if (RigidModel == null) return;
            foreach (var part in RigidModel.AllParts)
            {
                foreach (var hp in part.Hardpoints)
                {
                    if(!hardpoints.ContainsKey(hp.Definition.Name))
                        hardpoints.Add(hp.Definition.Name, hp);
                }
            }
        }

        public bool TryGetComponent<T>(out T component) where T : GameComponent
        {
            component = GetComponent<T>();
            return (component != null);
        }

		public T GetComponent<T>() where T : GameComponent
		{
            for (int i = 0; i < Components.Count; i++)
                if (Components[i] is T component)
                    return component;
			return null;
		}

        public T GetFirstChildComponent<T>() where T : GameComponent
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i].GetComponent<T>();
                if (c != null) return c;
            }
            return null;
        }

		public IEnumerable<T> GetChildComponents<T>() where T : GameComponent
		{
			for (int i = 0; i < Children.Count; i++)
			{
				var c = Children[i].GetComponent<T>();
				if (c != null) yield return c;
			}
		}

		public void Update(double time)
        {
            for (int i = 0; i < Children.Count; i++)
				Children[i].Update(time);
			for (int i = 0; i < Components.Count; i++)
				Components[i].Update(time);
		}

        public void RenderUpdate(double time)
        {
            var myPos = Vector3.Transform(Vector3.Zero, WorldTransform);
            RenderComponent?.Update(time, myPos, WorldTransform);
            for (int i = 0; i < Children.Count; i++)
                Children[i].RenderUpdate(time);
            foreach(var child in ExtraRenderers)
                child.Update(time, myPos, WorldTransform);
        }

        public void Register(PhysicsWorld physics)
        {
            Flags |= GameObjectFlags.Exists;
			foreach (var child in Children)
				child.Register(physics);
			foreach (var component in Components)
				component.Register(physics);
		}

		public GameWorld World;

		public GameWorld GetWorld()
		{
			if (World == null) return _parent?.GetWorld();
			return World;
		}

        public void PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool parentCull = false)
        {
            if(RenderComponent == null || RenderComponent.PrepareRender(camera,nr,sys, parentCull)) {
                //Guns etc. aren't drawn when parent isn't on LOD0
                parentCull = RenderComponent != null && RenderComponent.CurrentLevel > 0;
            }
            else {
                parentCull = true;
            }

            foreach (var child in Children) {
                child.PrepareRender(camera, nr, sys, parentCull);
            }

            foreach (var child in ExtraRenderers)
                child.PrepareRender(camera, nr, sys, false);
        }

        public List<ObjectRenderer> ExtraRenderers = new List<ObjectRenderer>();

		public void Unregister(PhysicsWorld physics)
        {
            Flags &= ~GameObjectFlags.Exists;
            foreach (var component in Components)
                component.Unregister(physics);
			foreach (var child in Children)
				child.Unregister(physics);
		}

		public bool HardpointExists(string hpname)
		{
			return hardpoints.ContainsKey(hpname);
		}

		public Hardpoint GetHardpoint(string hpname)
        {
            if (hpname == null)
                return null;
            Hardpoint tryget;
            if (hardpoints.TryGetValue(hpname, out tryget)) return tryget;
            return null;
		}

		public Vector3 InverseTransformPoint(Vector3 input)
		{
			var tf = WorldTransform;
            Matrix4x4.Invert(tf, out tf);
			return Vector3.Transform(input, tf);
		}

		public IEnumerable<Hardpoint> GetHardpoints()
		{
			return hardpoints.Values;
		}



		public override string ToString()
		{
			return string.Format("[{0}: {1}]", Nickname, Name);
		}
	}
}
