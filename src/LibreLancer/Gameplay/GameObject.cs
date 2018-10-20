// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
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
		public ObjectRenderer RenderComponent;
		public PhysicsComponent PhysicsComponent;
		public AnimationComponent AnimationComponent;
		public SystemObject SystemObject;

		public GameObject(Archetype arch, ResourceManager res, bool staticpos = false)
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
				InitWithDrawable(arch.Drawable, res, staticpos);
			}
		}
		public GameObject()
		{

		}
		public GameObject(IDrawable drawable, ResourceManager res, bool staticpos = false)
		{
			isstatic = false;
			InitWithDrawable(drawable, res, staticpos);
		}
        public GameObject(Ship ship, ResourceManager res)
        {
            InitWithDrawable(ship.Drawable, res, false);
            PhysicsComponent.Mass = ship.Mass;
            PhysicsComponent.Inertia = ship.RotationInertia;
        }
		public void UpdateCollision()
		{
			if (PhysicsComponent == null) return;
            PhysicsComponent.UpdateParts();
		}
		public ResourceManager Resources;
        void InitWithDrawable(IDrawable drawable, ResourceManager res, bool staticpos, bool havePhys = true)
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
                /*if (File.Exists(path))
				{
					SurFile sur = res.GetSur(path);
					var shapes = new List<CompoundSurShape.TransformedShape>();
					foreach (var part in CmpParts)
					{
						var crc = CrcTool.FLModelCrc(part.ObjectName);
						if (!sur.HasShape(crc))
						{
							FLLog.Warning("Sur", "No hitbox for " + part.ObjectName);
							continue;
						}
						var colshape = sur.GetShape(crc);
						if (part.Construct == null)
						{
							foreach (var s in colshape)
								shapes.Add(new CompoundSurShape.TransformedShape(s, Matrix3.Identity, Vector3.Zero));						}
						else
						{
							var tr = part.Construct.Transform;
							var pos = tr.ExtractTranslation();
							var q = tr.ExtractRotation(true);
							var rot = Matrix3.CreateFromQuaternion(q);
							foreach (var s in colshape)
								shapes.Add(new CompoundSurShape.TransformedShape(s, rot, pos) { Tag = part.Construct });
						}
					}
					collisionShape = new CompoundSurShape(shapes);
				}*/
			}

            if (havePhys && phys != null)
            {
                PhysicsComponent = phys;
                Components.Add(phys);
            }
			PopulateHardpoints(dr);
			if (isCmp)
                RenderComponent = new ModelRenderer(CmpParts, (dr as CmpFile)) { Name = name };
			else
                RenderComponent = new ModelRenderer(dr) { Name = name };
		}

		public GameObject(Equipment equip, Hardpoint hp, GameObject parent)
		{
			Parent = parent;
			Attachment = hp;
			if (equip is LightEquipment)
			{
				RenderComponent = new LightEquipRenderer((LightEquipment)equip);
			}
			if (equip is EffectEquipment)
			{
				RenderComponent = new ParticleEffectRenderer(((EffectEquipment)equip).Particles);
                Components.Add(new UpdateSParamComponent(this));
			}
			if (equip is ThrusterEquipment)
			{
				var th = (ThrusterEquipment)equip;
				InitWithDrawable(th.Model, parent.Resources, false, false);
				Components.Add(new ThrusterComponent(this, th));
			}
            if (equip is GunEquipment)
            {
                var gn = (GunEquipment)equip;
                InitWithDrawable(gn.Model, parent.Resources,false, false);
                Components.Add(new WeaponComponent(this, gn));
            }
            if (equip.LODRanges != null && RenderComponent != null) RenderComponent.LODRanges = equip.LODRanges;
            if(equip.HPChild != null) {
                if (hardpoints.TryGetValue(equip.HPChild, out Hardpoint hpchild))
                {
                    Transform = hpchild.Transform.Inverted();
                }
            }
            if(RenderComponent is ModelRenderer &&
                parent.RenderComponent != null
              )
            {
                if (parent.RenderComponent.LODRanges != null)
                {
                    RenderComponent.InheritCull = true;
                }
                else if (parent.RenderComponent is ModelRenderer)
                {
                    var mr = (ModelRenderer)parent.RenderComponent;
                    if (mr.Model != null && mr.Model.Switch2 != null)
                        RenderComponent.InheritCull = true;
                    if(mr.CmpParts != null)
                    {
                        Part parentPart = null;
                        if (hp.parent != null)
                            parentPart = mr.CmpParts.Find((o) => o.ObjectName == hp.parent.ChildName);
                        else
                            parentPart = mr.CmpParts.Find((o) => o.ObjectName == "Root");
                        if (parentPart.Model.Switch2 != null)
                            RenderComponent.InheritCull = true;
                    }
                }
            }
            //Optimisation: Don't re-calculate transforms every frame for static objects
            if(parent.isstatic && (hp == null || hp.IsStatic))
            {
                Transform = GetTransform();
                isstatic = true;
                StaticPosition = Transform.Transform(Vector3.Zero);
            }
		}

		public void SetLoadout(Dictionary<string, Equipment> equipment, List<Equipment> nohp)
		{
			foreach (var k in equipment.Keys)
			{
				var hp = GetHardpoint(k);
				Children.Add(new GameObject(equipment[k], hp, this));
			}
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
				var tr = GetTransform();
				RenderComponent.Update(time, isstatic ? StaticPosition : tr.Transform(Vector3.Zero), tr);
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