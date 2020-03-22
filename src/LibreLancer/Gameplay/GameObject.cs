// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Utf;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Physics;
using LibreLancer.GameData;
using LibreLancer.GameData.Items;
using Archs = LibreLancer.GameData.Archetypes;

namespace LibreLancer
{
	public class GameObject
	{
		//Object data
		public string Name;
		public string Nickname;
		public Hardpoint Attachment;
		Matrix4 _transform = Matrix4.Identity;
		public Matrix4 Transform
		{
			get
			{
                if (PhysicsComponent != null && PhysicsComponent.Body != null)
                    return PhysicsComponent.Body.Transform;
				return _transform;
			} set
			{
				_transform = value;
				if (PhysicsComponent != null && PhysicsComponent.Body != null)
				{
                    PhysicsComponent.Body.SetTransform(value);
				}
			}
		}
		public GameObject Parent;
		bool isstatic = false;
		public Vector3 StaticPosition;
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
        public bool IsStatic => isstatic;
        /// <summary>
        /// Don't call unless you're absolutely sure what you're doing!
        /// </summary>
        /// <param name="val">Sets a static position for the object (only works properly before Register())</param>
        public void SetStatic(bool val) => isstatic = val;
		public GameObject(Archetype arch, ResourceManager res, bool draw = true, bool staticpos = false)
		{
			isstatic = staticpos;
			if (arch is Archs.Sun)
			{
				RenderComponent = new SunRenderer((Archs.Sun)arch);
            }
			else
			{
				InitWithDrawable(arch.ModelFile.LoadFile(res), res, draw, staticpos);
			}
		}
		public GameObject()
		{

		}
        public static GameObject WithModel(ResolvedModel modelFile, bool draw, ResourceManager res)
        {
            var go = new GameObject();
            go.isstatic = false;
            go.InitWithDrawable(modelFile.LoadFile(res), res, draw, false, false);
            return go;
        }
		public GameObject(IDrawable drawable, ResourceManager res, bool draw = true, bool staticpos = false)
		{
			isstatic = false;
			InitWithDrawable(drawable, res, draw, staticpos);
		}
        public GameObject(Ship ship, ResourceManager res, bool draw = true)
        {
            InitWithDrawable(ship.ModelFile.LoadFile(res), res, draw, false);
            PhysicsComponent.Mass = ship.Mass;
            PhysicsComponent.Inertia = ship.RotationInertia;
        }

        public GameObject(string name, RigidModel model, ResourceManager res)
        {
            RigidModel = model;
            Resources = res;
            PopulateHardpoints();
            if (RigidModel != null)
            {
                RenderComponent = new ModelRenderer(RigidModel) { Name = name };
            }
        }
		public void UpdateCollision()
		{
			if (PhysicsComponent == null) return;
            PhysicsComponent.UpdateParts();
		}
        
        public void DisableCmpPart(string part)
        {
            if(RigidModel != null && RigidModel.Parts.TryGetValue(part, out var p))
            {
                p.Active = false;
                PhysicsComponent.DisablePart(p);
            }
        }
        
        public GameObject SpawnDebris(string part)
        {
            if (RigidModel != null && RigidModel.Parts.TryGetValue(part, out var srcpart))
            {
                var newpart = srcpart.Clone();
                var newmodel = new RigidModel()
                {
                    Root = newpart,
                    AllParts = new[] { newpart },
                    MaterialAnims = RigidModel.MaterialAnims,
                    Path = newpart.Path,
                };
                srcpart.Active = false;
                var tr = srcpart.LocalTransform * GetTransform();
                var obj = new GameObject($"{Name}$debris-{part}", newmodel, Resources);
                obj.Transform = tr;
                obj.World = World;
                obj.World.Objects.Add(obj);
                var pos0 = GetTransform().Transform(Vector3.Zero);
                var pos1 = tr.Transform(Vector3.Zero);
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
                PhysicsComponent.ChildDebris(obj, srcpart, mass, vec * initialforce);
            }
            return null;
        }
        public ResourceManager Resources;
        void InitWithDrawable(IDrawable drawable, ResourceManager res, bool draw, bool staticpos, bool havePhys = true)
		{
			Resources = res;
			dr = drawable;
            PhysicsComponent phys = null;
			bool isCmp = false;
            string name = "";
            if(draw) drawable.Initialize(res);
			if (dr is SphFile)
			{
				var radius = ((SphFile)dr).Radius;
                phys = new PhysicsComponent(this) { SphereRadius = radius };
                name = ((SphFile)dr).SideMaterialNames[0];
                RigidModel = ((SphFile) dr).CreateRigidModel(draw);
            }
			else if (dr is IRigidModelFile mdl)
			{
				//var mdl = dr as ModelFile;
                RigidModel = mdl.CreateRigidModel(draw);
				var path = Path.ChangeExtension(RigidModel.Path, "sur");
                name = Path.GetFileNameWithoutExtension(RigidModel.Path);
                if (File.Exists(path))
                    phys = new PhysicsComponent(this) { SurPath = path };
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

        public void SetLoadout(Dictionary<string, Equipment> equipment, List<Equipment> nohp)
		{
			foreach (var k in equipment.Keys)
                EquipmentObjectManager.InstantiateEquipment(this, Resources, RenderComponent != null, k, equipment[k]);
            foreach (var eq in nohp)
			{
				if (eq is AnimationEquipment)
				{
					var anm = (AnimationEquipment)eq;
					if(anm.Animation != null)
						AnimationComponent?.StartAnimation(anm.Animation);
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

		public T GetComponent<T>() where T : GameComponent
		{
			for (int i = 0; i < Components.Count; i++)
				if (Components[i] is T)
					return (T)Components[i];
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

		public void Update(TimeSpan time)
		{
            if (RenderComponent != null)
			{
                if (Parent == null || Parent.RenderUpdate(this)) {
                    var tr = GetTransform();
                    RenderComponent.Update(time, isstatic ? StaticPosition : tr.Transform(Vector3.Zero), tr);
                }
            }
			for (int i = 0; i < Children.Count; i++)
				Children[i].Update(time);
			for (int i = 0; i < Components.Count; i++)
				Components[i].Update(time);
		}
        
        public void FixedUpdate(TimeSpan time)
		{
			for ( int i = 0; i < Children.Count; i++)
				Children[i].FixedUpdate(time);
			for (int i = 0; i < Components.Count; i++)
				Components[i].FixedUpdate(time);
            if (!isstatic && PhysicsComponent != null && PhysicsComponent.Body != null)
			{
                Transform = PhysicsComponent.Body.Transform;
			}
		}

		public void Register(PhysicsWorld physics)
		{
			foreach (var child in Children)
				child.Register(physics);
			foreach (var component in Components)
				component.Register(physics);
		}

		public GameWorld World;

		public GameWorld GetWorld()
		{
			if (World == null) return Parent.GetWorld();
			return World;
		}

        protected bool RenderUpdate(GameObject child)
        {
            if (ForceRenderCheck.Contains(child.RenderComponent)) return true;
            return RenderComponent == null || RenderComponent.CurrentLevel == 0 || !child.RenderComponent.InheritCull;
        }
        public void PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            if(RenderComponent == null || RenderComponent.PrepareRender(camera,nr,sys))
            {
                //Guns etc. aren't drawn when parent isn't on LOD0
                var isZero = RenderComponent == null || RenderComponent.CurrentLevel == 0;
                foreach (var child in Children) {
                    if((child.RenderComponent != null && !child.RenderComponent.InheritCull) ||
                       isZero)
                    child.PrepareRender(camera, nr, sys);
                }
            }
            foreach (var child in ForceRenderCheck)
                child.PrepareRender(camera, nr, sys);
        }

        public List<ObjectRenderer> ForceRenderCheck = new List<ObjectRenderer>();

		public void Unregister(PhysicsWorld physics)
		{
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
			var tf = GetTransform();
			tf.Invert();
			return VectorMath.Transform(input, tf);
		}

		public IEnumerable<Hardpoint> GetHardpoints()
		{
			return hardpoints.Values;
		}

		public Matrix4 GetTransform()
		{
			if (isstatic)
				return Transform;
			var tr = Transform;
			if (Attachment != null)
				tr *= Attachment.Transform;
			if (Parent != null)
				tr *= Parent.GetTransform();
			return tr;
		}

		public override string ToString()
		{
			return string.Format("[{0}: {1}]", Nickname, Name);
		}
	}
}