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
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.GameData;
namespace LibreLancer
{
	public class ModelRenderer : ObjectRenderer
	{
		public Matrix4 World { get; private set; }
		public ModelFile Model { get; private set; }
		public CmpFile Cmp { get; private set; }
		public SphFile Sph { get; private set; }
		public NebulaRenderer Nebula;
		float radiusAtmosphere;
		Vector3 pos;
		bool inited = false;
		SystemRenderer sysr;
		public ModelRenderer (IDrawable drawable)
		{
			if (drawable is ModelFile)
				Model = drawable as ModelFile;
			else if (drawable is CmpFile)
				Cmp = drawable as CmpFile;
			else if (drawable is SphFile)
				Sph = drawable as SphFile;
		}

		public override void Update(TimeSpan elapsed, Vector3 position, Matrix4 transform)
		{
			if (sysr == null)
				return;
			World = transform;
			if (Nebula == null || pos != position)
			{
				pos = position;
				Nebula = sysr.ObjectInNebula(position);
			}
		}

		public override void Register(SystemRenderer renderer)
		{
			sysr = renderer;
			sysr.Objects.Add(this);
			if (!inited)
			{
				if (Model != null && Model.Levels.ContainsKey(0))
					Model.Initialize(sysr.Game.ResourceManager);
				else if (Cmp != null)
					Cmp.Initialize(sysr.Game.ResourceManager);
				else if (Sph != null)
				{
					Sph.Initialize(sysr.Game.ResourceManager);
					if (Sph.SideMaterials.Length > 6)
						radiusAtmosphere = Sph.Radius * Sph.SideMaterials[6].Scale;
					else
						radiusAtmosphere = Sph.Radius;
				}
				inited = true;
			}
		}

		public override void Unregister()
		{
			sysr.Objects.Remove(this);
			sysr = null;
		}

		public override void Draw(ICamera camera, CommandBuffer commands, Lighting lights, NebulaRenderer nr)
		{
			if (sysr == null)
				return;
			if (Nebula != null && nr != Nebula)
				return;
			if (Model != null) {
				if (Model.Levels.ContainsKey (0)) {
					Model.Update(camera, TimeSpan.Zero);
					var center = VectorMath.Transform(Model.Levels[0].Center, World);
					var bsphere = new BoundingSphere(
						center,
						Model.Levels[0].Radius
					);
					if (camera.Frustum.Intersects(bsphere)) {
						var lighting = RenderHelpers.ApplyLights(lights, center, Model.Levels[0].Radius, nr);
						var r = Model.Levels [0].Radius + lighting.FogRange.Y;
						if (!lighting.FogEnabled || VectorMath.DistanceSquared(camera.Position, center) <= (r * r))
							Model.DrawBuffer(commands, World, lighting);
					}
				}
			} else if (Cmp != null) {
				Cmp.Update(camera, TimeSpan.Zero);
				foreach (Part p in Cmp.Parts.Values)
				{
					var model = p.Model;
					Matrix4 w = World;
					if (p.Construct != null)
						w = p.Construct.Transform * World;
					if (model.Levels.ContainsKey(0))
					{

						var center = VectorMath.Transform(model.Levels[0].Center, w);
						var bsphere = new BoundingSphere(
							center,
							model.Levels[0].Radius
						);
						if (camera.Frustum.Intersects(bsphere))
						{
							var lighting = RenderHelpers.ApplyLights(lights, center, model.Levels[0].Radius, nr);
							var r = model.Levels [0].Radius + lighting.FogRange.Y;
							if (!lighting.FogEnabled || VectorMath.DistanceSquared(camera.Position, center) <= (r * r))
								model.DrawBuffer(commands, w, lighting);
						}
					}
				}
			} else if (Sph != null) {
				Sph.Update(camera, TimeSpan.Zero);
				var bsphere = new BoundingSphere(
					pos,
					radiusAtmosphere);
				if (camera.Frustum.Intersects(bsphere))
				{
					var l = RenderHelpers.ApplyLights(lights, pos, Sph.Radius, nr);
					var r = Sph.Radius + l.FogRange.Y;
					if(!l.FogEnabled || VectorMath.DistanceSquared(camera.Position, pos) <= (r * r))
						Sph.DrawBuffer(commands, World, l);
				}
			}
		}
	}
}

