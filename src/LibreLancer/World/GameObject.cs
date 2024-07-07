// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using LibreLancer.Data.Solar;
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
        Missile,
        Waypoint,
        Debris
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
            var fwd = Vector3.Transform(-Vector3.UnitZ, Parent.PhysicsComponent.Body.Orientation);
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
        Cloaked = 1 << 5, //Bad
        Player = 1 << 6,
        Reputations = Neutral | Friendly | Hostile,
    }


	public class GameObject
	{
        //Static
        private static int _unique = 0;
        public static object ClientPlayerTag = new object();

        //Public Fields
        public readonly int Unique = Interlocked.Increment(ref _unique);
		public ObjectName Name;
        public GameObjectFlags Flags;
        public ShipFormation Formation = null;
        public GameObjectKind Kind = GameObjectKind.None;
        public object Tag;
        public string ArchetypeName;
        public int NetID;
        public Hardpoint _attachment;
        public SystemObject SystemObject;
        public RigidModel RigidModel;
        public ResourceManager Resources;
        public GameWorld World;
        public List<ObjectRenderer> ExtraRenderers = new List<ObjectRenderer>();

        //Private Fields
        private HashSet<string> disabledParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Transform3D _localTransform = Transform3D.Identity;
        private string _nickname;
        private bool transformDirty = false;
        Transform3D worldTransform = Transform3D.Identity;
        private GameObject _parent;
        IDrawable dr;
        Dictionary<string, Hardpoint> hardpoints = new Dictionary<string, Hardpoint>(StringComparer.OrdinalIgnoreCase);
        public List<GameObject> Children = new List<GameObject>();
        public Data.Solar.CollisionGroup[] CollisionGroups;
        private Dictionary<Type, GameComponent> componentLookup = new Dictionary<Type, GameComponent>();
        private List<GameComponent> components = new List<GameComponent>();
        //Components
        public ObjectRenderer RenderComponent { get; set; }
        public PhysicsComponent PhysicsComponent { get; private set; }
        public AnimationComponent AnimationComponent { get; set; }
        //Properties
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

		public Transform3D LocalTransform
		{
			get
			{
                return _localTransform;
			}
		}

        public void SetLocalTransform(Transform3D tr, bool phys = false)
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

        Transform3D CalculateTransform()
        {
            var tr = LocalTransform;
            if (_attachment != null)
                tr *= _attachment.Transform;
            if (_parent != null)
                tr *= _parent.WorldTransform;
            return tr;
        }

        public Transform3D WorldTransform
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

        public void AddComponent<T>(T component) where T: GameComponent
        {
            componentLookup.TryAdd(typeof(T), component);
            components.Add(component);
            if (typeof(T).BaseType != typeof(GameComponent)) {
                componentLookup.TryAdd(typeof(T).BaseType, component);
            }
        }

        public bool TryGetComponent<T>(out T component) where T : GameComponent
        {
            if (componentLookup.TryGetValue(typeof(T), out var c))
            {
                component = Unsafe.As<T>(c);
                return true;
            }
            component = null;
            return false;
        }

        public T GetComponent<T>() where T : GameComponent
        {
            TryGetComponent(out T c);
            return c;
        }


        public void RemoveComponent<T>(T component) where T : GameComponent
        {
            components.Remove(component);
            componentLookup.Remove(typeof(T));
            if (typeof(T).BaseType != typeof(GameComponent))
            {
                componentLookup.Remove(typeof(T).BaseType);
            }
        }

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
            InitWithArchetype(arch, res, draw, phys);
		}

        public void InitWithArchetype(Archetype arch, ResourceManager res, bool draw = true, bool phys = true)
        {
            Kind = arch.Type == ArchetypeType.waypoint ? GameObjectKind.Waypoint : GameObjectKind.Solar;
            if (arch is Archs.Sun)
            {
                RenderComponent = new SunRenderer((Archs.Sun)arch);
            }
            else
            {
                InitWithModel(arch.ModelFile.LoadFile(res), res, draw, phys);
            }
        }

		public GameObject()
		{

		}
        public static GameObject WithModel(ResolvedModel modelFile, bool draw, ResourceManager res)
        {
            var go = new GameObject();
            go.InitWithModel(modelFile.LoadFile(res), res, draw,  false);
            return go;
        }
		public GameObject(ModelResource model, ResourceManager res, bool draw = true,  bool phys = true)
		{
            InitWithModel(model, res, draw,  phys);
        }
        public GameObject(Ship ship, ResourceManager res, bool draw = true, bool phys = false)
        {
            InitWithModel(ship.ModelFile.LoadFile(res), res, draw, phys);
            ArchetypeName = ship.Nickname;
            Kind = GameObjectKind.Ship;
            if (RenderComponent is ModelRenderer mr)
            {
                mr.LODRanges = ship.LODRanges;
            }
            if (PhysicsComponent != null)
            {
                PhysicsComponent.Mass = ship.Mass;
                PhysicsComponent.Inertia = ship.RotationInertia;
            }
        }

        public GameObject(RigidModel model, CollisionMeshHandle collider, ResourceManager res, string partName, float mass, bool draw)
        {
            RigidModel = model;
            Resources = res;
            PopulateHardpoints();
            if (draw && RigidModel != null)
            {
                RenderComponent = new ModelRenderer(RigidModel);
            }
            uint plainCrc = 0;
            if (!string.IsNullOrEmpty(partName)) plainCrc = CrcTool.FLModelCrc(partName);
            if (collider.Valid)
            {
                PhysicsComponent = new PhysicsComponent(this)
                {
                    SurPath = collider,
                    Mass = mass,
                    PlainCrc = plainCrc
                };
                AddComponent(PhysicsComponent);
            }
        }
		public void UpdateCollision()
        {
            for (int i = 0; i < Children.Count; i++) Children[i].transformDirty = true;
			if (PhysicsComponent == null) return;
            PhysicsComponent.UpdateParts();
        }

        public bool DisableCmpPart(string part)
        {
            if(RigidModel != null && RigidModel.Parts.TryGetValue(part, out var p))
            {
                if (disabledParts.Contains(part))
                    return false;
                p.Active = false;
                PhysicsComponent.DisablePart(p);
                World?.Server?.PartDisabled(this, part);
                disabledParts.Add(part);
                for (int i = Children.Count - 1; i >= 0; i--)
                {
                    var child = Children[i];
                    if (!(child.Attachment?.Parent?.Active ?? true))
                    {
                        Children.RemoveAt(i);
                    }
                }
                return true;
            }
            return false;
        }

        public void SpawnDebris(string part)
        {
            if (World?.Server == null) {
                throw new Exception("Server-only code");
            }
            if (RigidModel != null && RigidModel.Parts.TryGetValue(part, out var srcpart))
            {
                if (!DisableCmpPart(part))
                    return;
                var tr = srcpart.LocalTransform * WorldTransform;
                var vec = (tr.Position - WorldTransform.Position).Normalized();
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
        public void InitWithModel(ModelResource drawable, ResourceManager res, bool draw, bool havePhys = true)
		{
			Resources = res;
			dr = drawable.Drawable;
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
                if (drawable.Collision.Valid)
                    phys = new PhysicsComponent(this) { SurPath = drawable.Collision, Collidable = Kind != GameObjectKind.Waypoint };
                if (RigidModel.Animation != null)
                {
                    AnimationComponent = new AnimationComponent(this, RigidModel.Animation);
                    AddComponent(AnimationComponent);
                }
			}
            if (havePhys && phys != null)
            {
                PhysicsComponent = phys;
                AddComponent(phys);
            }
            PopulateHardpoints();
            if (draw && RigidModel != null)
            {
                RenderComponent = new ModelRenderer(RigidModel);
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

        public T GetFirstChildComponent<T>() where T : GameComponent
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i].GetComponent<T>();
                if (c != null) return c;
            }
            return null;
        }

        public struct ChildComponentEnumerator<T> : IEnumerator<T> where T : GameComponent
        {
            private int i;
            private GameObject obj;

            public ChildComponentEnumerator(GameObject obj)
            {
                this.obj = obj;
                i = 0;
            }

            public bool MoveNext()
            {
                if (i >= obj.Children.Count)
                {
                    Current = null;
                    return false;
                }
                T result = null;
                while (i < obj.Children.Count && (result = obj.Children[i].GetComponent<T>()) == null)
                    i++;
                i++;
                Current = result;
                return result != null;
            }

            public void Reset()
            {
                i = 0;
                Current = null;
            }

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() => Reset();
        }


		public StructEnumerable<T, ChildComponentEnumerator<T>> GetChildComponents<T>() where T : GameComponent
        {
            return new(new ChildComponentEnumerator<T>(this));
        }

		public void Update(double time)
        {
            for (int i = 0; i < Children.Count; i++)
				Children[i].Update(time);
			for (int i = 0; i < components.Count; i++)
				components[i].Update(time);
		}

        public void RenderUpdate(double time)
        {
            RenderComponent?.Update(time, WorldTransform.Position, WorldTransform.Matrix());
            for (int i = 0; i < Children.Count; i++)
                Children[i].RenderUpdate(time);
            foreach(var child in ExtraRenderers)
                child.Update(time, WorldTransform.Position, WorldTransform.Matrix());
        }

        public void Register(PhysicsWorld physics)
        {
			foreach (var child in Children)
				child.Register(physics);
			foreach (var component in components)
				component.Register(physics);
            Flags |= GameObjectFlags.Exists;
		}


		public GameWorld GetWorld()
		{
			if (World == null) return _parent?.GetWorld();
			return World;
		}

        public void PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool parentCull = false)
        {
            if(RenderComponent == null || RenderComponent.PrepareRender(camera,nr,sys, parentCull)) {
                //Guns etc. aren't drawn when parent isn't on LOD0
                parentCull = RenderComponent is ModelRenderer {CurrentLevel: > 0};
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


        public void ClearAll(PhysicsWorld physics)
        {
            Unregister(physics);
            ExtraRenderers.Clear();
            componentLookup.Clear();
            components.Clear();
            Children.Clear();
            hardpoints.Clear();
            disabledParts.Clear();
            CollisionGroups = null;
            _parent = null;
            transformDirty = true;
            _localTransform = Transform3D.Identity;
            Nickname = null;
            Name = null;
            Flags = 0;
            Formation = null;
            World = null;
            Kind = GameObjectKind.None;
            NetID = 0;
            Tag = null;
            ArchetypeName = null;
            _attachment = null;
            SystemObject = null;
            RigidModel = null;
            Resources = null;
            RenderComponent = null;
            PhysicsComponent = null;
            AnimationComponent = null;
        }

		public void Unregister(PhysicsWorld physics)
        {
            foreach (var component in components)
                component.Unregister(physics);
			foreach (var child in Children)
				child.Unregister(physics);
            Flags &= ~GameObjectFlags.Exists;
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

        public Vector3 InverseTransformPoint(Vector3 input) => WorldTransform.InverseTransform(input);

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
