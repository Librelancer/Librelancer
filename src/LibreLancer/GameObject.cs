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
using LibreLancer.Utf.Cmp;
using LibreLancer.GameData;
using Archs = LibreLancer.GameData.Archetypes;
namespace LibreLancer
{
	public class GameObject
	{
		public string Name;
		public string Nickname;
		public Hardpoint Attachment;
		public Matrix4 Transform;
		public Vector3 Position;
		public GameObject Parent;
		public List<GameObject> Children = new List<GameObject>();
		IDrawable dr;
		IObjectRenderer renderComponent;
		Dictionary<string, Hardpoint> hardpoints = new Dictionary<string, Hardpoint>(StringComparer.OrdinalIgnoreCase);

		public GameObject(Archetype arch)
		{
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

		void PopulateHardpoints(IDrawable drawable, Matrix4? transform = null)
		{
			if (drawable is CmpFile)
			{
				var cmp = (CmpFile)drawable;
				foreach (var part in cmp.Parts.Values)
				{
					PopulateHardpoints(part.Model, part.Construct != null ? part.Construct.Transform : Matrix4.Identity);
				}
			}
			else if (drawable is ModelFile)
			{
				var model = (ModelFile)drawable;
				var tr = transform ?? Matrix4.Identity;
				foreach (var hpdef in model.Hardpoints)
				{
					hardpoints.Add(hpdef.Name, new Hardpoint(hpdef, tr));
				}
			}
		}

		public void Update(TimeSpan time)
		{
			if (renderComponent != null)
			{
				renderComponent.Update(time, Position, GetTransform());
			}
			foreach (var c in Children)
				c.Update(time);
		}

		public void Register(SystemRenderer renderer)
		{
			renderComponent.Register(renderer);
		}

		public void Unregister()
		{
			renderComponent.Unregister();
		}

		public Hardpoint GetHardpoint(string hpname)
		{
			return hardpoints[hpname];
		}

		public Matrix4 GetTransform()
		{
			var tr = Matrix4.Identity;
			if(Parent != null)
				tr = Parent.GetTransform();
			if (Attachment != null)
				tr = Attachment.Transform * tr;
			return Transform * tr;
		}
	}
}

