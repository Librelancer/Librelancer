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
using System.Collections.Generic;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using LibreLancer.GameData;
using LibreLancer.GameData.Items;
using Archs = LibreLancer.GameData.Archetypes;
namespace LibreLancer
{
	public class GameObject
	{
		public string Name;
		public string Nickname;
		public Hardpoint Attachment;
		public Matrix4 Transform = Matrix4.Identity;
		public GameObject Parent;
		public List<GameObject> Children = new List<GameObject>();
		IDrawable dr;
		IObjectRenderer renderComponent;
		Dictionary<string, Hardpoint> hardpoints = new Dictionary<string, Hardpoint>(StringComparer.OrdinalIgnoreCase);
		bool staticpos = false;
		public Vector3 StaticPosition;
		public GameObject(Archetype arch, bool staticpos = false)
		{
			this.staticpos = staticpos;
			if (arch is Archs.Sun)
			{
				renderComponent = new SunRenderer((Archs.Sun)arch);
			}
			else
			{
				dr = arch.Drawable;
				PopulateHardpoints(dr);
				renderComponent = new ModelRenderer(dr);
			}
		}

		public GameObject(Equipment equip, Hardpoint hp, GameObject parent)
		{
			Parent = parent;
			Attachment = hp;
			if (equip is LightEquipment)
			{
				renderComponent = new LightEquipRenderer((LightEquipment)equip);
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
			if (renderComponent != null)
			{
				var tr = GetTransform();
				renderComponent.Update(time, staticpos ? StaticPosition : tr.Transform(Vector3.Zero), tr);
			}
			for (int i = 0; i < Children.Count; i++)
				Children[i].Update(time);
		}

		public void Register(SystemRenderer renderer)
		{
			if(renderComponent != null)
				renderComponent.Register(renderer);
			foreach (var child in Children)
				child.Register(renderer);
		}

		public void Unregister()
		{
			if(renderComponent != null)
				renderComponent.Unregister();
			foreach (var child in Children)
				child.Unregister();
		}

		public Hardpoint GetHardpoint(string hpname)
		{
			return hardpoints[hpname];
		}

		public Matrix4 GetTransform()
		{
			if (staticpos)
				return Transform;
			var tr = Matrix4.Identity;
			if (Parent != null)
				tr = Parent.GetTransform();
			if (Attachment != null)
				tr = Attachment.Transform * tr;
			return Transform * tr;
		}
	}
}

