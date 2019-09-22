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
		public ConstructCollection CmpConstructs;
		public List<Part> CmpParts = new List<Part>();
		Dictionary<string, Hardpoint> hardpoints = new Dictionary<string, Hardpoint>(StringComparer.OrdinalIgnoreCase);
		//Components
		public List<GameObject> Children = new List<GameObject>();
		public List<GameComponent> Components = new List<GameComponent>();
        public Data.Solar.CollisionGroup[] CollisionGroups;
		public ObjectRenderer RenderComponent;
		public PhysicsComponent PhysicsComponent;
		public AnimationComponent AnimationComponent;
		public SystemObject SystemObject;

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
				//TODO: You can't collide with a sun
				//PhysicsComponent = new RigidBody(new SphereShape((((Archs.Sun)arch).Radius)));
				//PhysicsComponent.IsStatic = true;
				//PhysicsComponent.Tag = this;
			}
			else
			{
				InitWithDrawable(arch.Drawable, res, draw, staticpos);
			}
		}
		public GameObject()
		{

		}
        public static GameObject WithModel(IDrawable drawable, ResourceManager res)
        {
            var go = new GameObject();
            go.isstatic = false;
            go.InitWithDrawable(drawable, res, true, false, false);
            return go;
        }
		public GameObject(IDrawable drawable, ResourceManager res, bool draw = true, bool staticpos = false)
		{
			isstatic = false;
			InitWithDrawable(drawable, res, draw, staticpos);
		}
        public GameObject(Ship ship, ResourceManager res, bool draw = true)
        {
            InitWithDrawable(ship.Drawable, res, draw, false);
            PhysicsComponent.Mass = ship.Mass;
            PhysicsComponent.Inertia = ship.RotationInertia;
        }
		public void UpdateCollision()
		{
			if (PhysicsComponent == null) return;
            PhysicsComponent.UpdateParts();
		}
        public void DisableCmpPart(string part)
        {
            if (CmpParts == null) return;
            for (int i = 0; i < CmpParts.Count; i++)
            {
                if (CmpParts[i].ObjectName.Equals(part, StringComparison.OrdinalIgnoreCase))
                {
                    PhysicsComponent.DisablePart(CmpParts[i]);
                    CmpParts.RemoveAt(i);
                    return;
                }
            }
        }
        public GameObject SpawnDebris(string part)
        {
            if (CmpParts == null) return null;
            for (int i = 0; i < CmpParts.Count; i++)
            {
                var p = CmpParts[i];
                if (p.ObjectName.Equals(part, StringComparison.OrdinalIgnoreCase))
                {
                    var tr = p.GetTransform(GetTransform());
                    CmpParts.RemoveAt(i);
                    var obj = new GameObject(p.Model, Resources, RenderComponent != null, false);
                    obj.Transform = tr;
                    obj.World = World;
                    obj.World.Objects.Add(obj);
                    var pos0 = GetTransform().Transform(Vector3.Zero);
                    var pos2 = p.GetTransform(GetTransform()).Transform(Vector3.Zero);
                    var vec = (pos2 - pos0).Normalized();
                    var initialforce = 100f;
                    var mass = 50f;
                    if(CollisionGroups != null)
                    {
                        var cg = CollisionGroups.FirstOrDefault(x => x.obj.Equals(part, StringComparison.OrdinalIgnoreCase));
                        if(cg != null)
                        {
                            mass = cg.Mass;
                            initialforce = cg.ChildImpulse;
                        }
                    }
                    PhysicsComponent.ChildDebris(obj, p, mass,  vec * initialforce);
                    return obj;
                }
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
			if (dr is SphFile)
			{
				var radius = ((SphFile)dr).Radius;
                phys = new PhysicsComponent(this) { SphereRadius = radius };
                name = ((SphFile)dr).SideMaterialNames[0];
			}
			else if (dr is ModelFile)
			{
				var mdl = dr as ModelFile;
				var path = Path.ChangeExtension(mdl.Path, "sur");
                name = Path.GetFileNameWithoutExtension(mdl.Path);
                if (File.Exists(path))
                    phys = new PhysicsComponent(this) { SurPath = path };
			}
			else if (dr is CmpFile)
			{
				isCmp = true;
				var cmp = dr as CmpFile;
				CmpParts = new List<Part>();
				CmpConstructs = cmp.Constructs.CloneAll();
				foreach (var part in cmp.Parts)
				{
					CmpParts.Add(part.Clone(CmpConstructs));
				}
				if (cmp.Animation != null)
				{
					AnimationComponent = new AnimationComponent(this, cmp.Animation);
					Components.Add(AnimationComponent);
				}
				var path = Path.ChangeExtension(cmp.Path, "sur");
                name = Path.GetFileNameWithoutExtension(cmp.Path);
                if (File.Exists(path))
                    phys = new PhysicsComponent(this) { SurPath = path };
			}

            if (havePhys && phys != null)
            {
                PhysicsComponent = phys;
                Components.Add(phys);
            }
			PopulateHardpoints(dr);
            if (draw)
            {
                if (isCmp)
                    RenderComponent = new ModelRenderer(CmpParts, (dr as CmpFile)) { Name = name };
                else
                    RenderComponent = new ModelRenderer(dr) { Name = name };
            }
		}

        public void SetLoadout(Dictionary<string, Equipment> equipment, List<Equipment> nohp)
		{
			foreach (var k in equipment.Keys)
                EquipmentObjectManager.InstantiateEquipment(this, Resources, k, equipment[k]);
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


		void PopulateHardpoints(IDrawable drawable, AbstractConstruct transform = null)
		{
			if (drawable is CmpFile)
			{
				foreach (var part in CmpParts)
				{
					PopulateHardpoints(part.Model, part.Construct);
				}
			}
			else if (drawable is ModelFile)
			{
				var model = (ModelFile)drawable;
				foreach (var hpdef in model.Hardpoints)
				{
					//Workaround broken models
					if(!hardpoints.ContainsKey(hpdef.Name))
						hardpoints.Add(hpdef.Name, new Hardpoint(hpdef, transform));
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
			for (int i = 0; i < Children.Count; i++)
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