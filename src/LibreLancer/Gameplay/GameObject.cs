/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using System.Collections.Generic;
using LibreLancer.Utf;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Sur;
using LibreLancer.GameData;
using LibreLancer.GameData.Items;
using Archs = LibreLancer.GameData.Archetypes;
using Jitter;
using Jitter.LinearMath;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;

namespace LibreLancer
{
	public class GameObject
	{
		public string Name;
		public string Nickname;
		public Hardpoint Attachment;
		Matrix4 _transform = Matrix4.Identity;
		public Matrix4 Transform
		{
			get
			{
				return _transform;
			} set
			{
				_transform = value;
				if (PhysicsComponent != null)
				{
					PhysicsComponent.Position = _transform.ExtractTranslation().ToJitter();
				}
			}
		}
		public GameObject Parent;
		public List<GameObject> Children = new List<GameObject>();
		public List<GameComponent> Components = new List<GameComponent>();
		IDrawable dr;
		ObjectRenderer renderComponent;
		Dictionary<string, Hardpoint> hardpoints = new Dictionary<string, Hardpoint>(StringComparer.OrdinalIgnoreCase);
		bool isstatic = false;
		public Vector3 StaticPosition;
		public RigidBody PhysicsComponent;
		public GameObject(Archetype arch, ResourceManager res, bool staticpos = false)
		{
			isstatic = staticpos;
			if (arch is Archs.Sun)
			{
				renderComponent = new SunRenderer((Archs.Sun)arch);
			}
			else
			{
				InitWithDrawable(arch.Drawable, res, staticpos);
			}
		}
		public GameObject(IDrawable drawable, ResourceManager res, bool staticpos = false)
		{
			isstatic = staticpos;
			InitWithDrawable(drawable, res, staticpos);
		}
		void InitWithDrawable(IDrawable drawable, ResourceManager res, bool staticpos)
		{
			dr = drawable;
			Shape collisionShape = null;
			if (dr is SphFile)
			{
				var radius = ((SphFile)dr).Radius;
				collisionShape = new SphereShape(radius);
			}
			else if (dr is ModelFile)
			{
				var mdl = dr as ModelFile;
				var path = Path.ChangeExtension(mdl.Path, "sur");
				if (File.Exists(path))
				{
					SurFile sur = res.GetSur(path);
					collisionShape = sur.GetShape(0);
				}
			}
			else if (dr is CmpFile)
			{
				var cmp = dr as CmpFile;
				var path = Path.ChangeExtension(cmp.Path, "sur");
				if (File.Exists(path))
				{
					SurFile sur = res.GetSur(path);
					var shapes = new List<CompoundSurShape.TransformedShape>();
					foreach (var part in cmp.Parts.Values)
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
							shapes.Add(new CompoundSurShape.TransformedShape(colshape, JMatrix.Identity, JVector.Zero));
						}
						else
						{
							var tr = part.Construct.Transform;
							var pos = tr.ExtractTranslation().ToJitter();
							var q = tr.ExtractRotation(true);
							var rot = JMatrix.CreateFromQuaternion(new JQuaternion(q.X, q.Y, q.Z, q.W));
							shapes.Add(new CompoundSurShape.TransformedShape(colshape, rot, pos));
						}
					}
					collisionShape = new CompoundSurShape(shapes);
				}
			}
			if (collisionShape != null)
			{
				PhysicsComponent = new RigidBody(collisionShape);
				PhysicsComponent.IsStatic = staticpos;
			}
			PopulateHardpoints(dr);
			renderComponent = new ModelRenderer(dr);
		}
		public GameObject(Equipment equip, Hardpoint hp, GameObject parent)
		{
			Parent = parent;
			Attachment = hp;
			if (equip is LightEquipment)
			{
				renderComponent = new LightEquipRenderer((LightEquipment)equip);
			}
			if (equip is EffectEquipment)
			{
				renderComponent = new ParticleEffectRenderer(((EffectEquipment)equip).Particles);
			}
            //Optimisation: Don't re-calculate transforms every frame for static objects
            if(parent.isstatic && hp.IsStatic)
            {
                Transform = GetTransform();
                isstatic = true;
                StaticPosition = Transform.Transform(Vector3.Zero);
            }
		}

		public void SetLoadout(Dictionary<string, Equipment> equipment)
		{
			foreach (var k in equipment.Keys)
			{
				var hp = GetHardpoint(k);
				Children.Add(new GameObject(equipment[k], hp, this));
			}
		}


		void PopulateHardpoints(IDrawable drawable, AbstractConstruct transform = null)
		{
			if (drawable is CmpFile)
			{
				var cmp = (CmpFile)drawable;
				foreach (var part in cmp.Parts.Values)
				{
					PopulateHardpoints(part.Model, part.Construct);
				}
			}
			else if (drawable is ModelFile)
			{
				var model = (ModelFile)drawable;
				foreach (var hpdef in model.Hardpoints)
				{
					hardpoints.Add(hpdef.Name, new Hardpoint(hpdef, transform));
				}
			}
		}

		public void Update(TimeSpan time)
		{
			if (PhysicsComponent != null && !isstatic)
			{
				Transform = PhysicsComponent.Orientation.ToOpenTK() * Matrix4.CreateTranslation(PhysicsComponent.Position.ToOpenTK());
			}
			if (renderComponent != null)
			{
				var tr = GetTransform();
				renderComponent.Update(time, isstatic ? StaticPosition : tr.Transform(Vector3.Zero), tr);
			}
			for (int i = 0; i < Children.Count; i++)
				Children[i].Update(time);
			for (int i = 0; i < Components.Count; i++)
				Components[i].Update(time);
		}

		public void Register(SystemRenderer renderer, World physics)
		{
			if(renderComponent != null)
				renderComponent.Register(renderer);
			if (PhysicsComponent != null)
				physics.AddBody(PhysicsComponent);
			foreach (var child in Children)
				child.Register(renderer, physics);
			foreach (var component in Components)
				component.Register(renderer, physics);
		}

		public void Unregister(World physics)
		{
			if(renderComponent != null)
				renderComponent.Unregister();
			if (PhysicsComponent != null)
				physics.RemoveBody(PhysicsComponent);
			foreach (var child in Children)
				child.Unregister(physics);
		}

		public Hardpoint GetHardpoint(string hpname)
		{
			return hardpoints[hpname];
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
	}
}

