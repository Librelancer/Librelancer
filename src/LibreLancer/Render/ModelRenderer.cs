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
using LibreLancer.Utf.Mat;
using DfmFile = LibreLancer.Utf.Dfm.DfmFile;
using LibreLancer.GameData;
namespace LibreLancer
{
	public class ModelRenderer : ObjectRenderer
	{
		public Matrix4 World { get; private set; }
		public ModelFile Model { get; private set; }
		public CmpFile Cmp { get; private set; }
		public DfmFile Dfm { get; private set; }
		public List<Part> CmpParts { get; private set; }
		public int LightGroup = 0;
		CmpFile _parentCmp;
		public SphFile Sph { get; private set; }
		public NebulaRenderer Nebula;
		float radiusAtmosphere;
		Vector3 pos;
		bool inited = false;
		SystemRenderer sysr;

		public ModelRenderer(IDrawable drawable)
		{
			if (drawable is ModelFile)
				Model = drawable as ModelFile;
			else if (drawable is CmpFile)
				Cmp = drawable as CmpFile;
			else if (drawable is SphFile)
				Sph = drawable as SphFile;
			else if (drawable is DfmFile)
				Dfm = drawable as DfmFile;
		}
		public ModelRenderer(List<Part> drawable, CmpFile parent)
		{
			CmpParts = drawable;
			_parentCmp = parent;
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
				if (Dfm != null)
					Dfm.Initialize(sysr.Game.ResourceManager);
				if (Model != null && Model.Levels.Length > 0)
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

		VMeshRef GetLevel(ModelFile file, Vector3 center, Vector3 camera)
		{
			float[] ranges = LODRanges ?? file.Switch2;
			if (ranges == null) return file.Levels[0];
			var dsq = VectorMath.DistanceSquared(center, camera);
			var lvl = file.Levels[0];
			for (int i = 0; i < ranges.Length; i++)
			{
				var d = ranges[i];
				if (i > 0 && ranges[i] < ranges[i - 1]) break;
				if (dsq < (d * sysr.LODMultiplier) * (d * sysr.LODMultiplier)) break;
				if (i >= file.Levels.Length) return null;
				lvl = file.Levels[i];
			}
			return lvl;
		}

		public override bool OutOfView(ICamera camera)
		{
			if (Model != null)
			{
				if (Model.Levels.Length != 0)
				{
					var center = VectorMath.Transform(Model.Levels[0].Center, World);
					var lvl = GetLevel(Model, center, camera.Position);
					if (lvl == null) return true;
					var bsphere = new BoundingSphere(
						center,
						Model.Levels[0].Radius
					);
					if (!camera.Frustum.Intersects(bsphere)) return true; //Culled
				}
			}
			else if (Cmp != null || CmpParts != null)
			{
				//Check if -something- renders
				bool doCull = true;
				var partCol = (IEnumerable<Part>)CmpParts ?? Cmp.Parts.Values;
				foreach (Part p in partCol)
				{
					var model = p.Model;
					Matrix4 w = World;
					if (p.Construct != null)
						w = p.Construct.Transform * World;
					if (model.Levels.Length > 0)
					{
						var center = VectorMath.Transform(model.Levels[0].Center, w);
						var lvl = GetLevel(model, center, camera.Position);
						if (lvl == null) continue;
						var bsphere = new BoundingSphere(
							center,
							model.Levels[0].Radius
						);
						if (camera.Frustum.Intersects(bsphere))
						{
							doCull = false;
							break;
						}
					}
				}
				return doCull;
			}
			else if (Sph != null)
			{
				var bsphere = new BoundingSphere(
					pos,
					radiusAtmosphere);
				if (!camera.Frustum.Intersects(bsphere)) return true;
			}
			return false;
		}
		public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
			if (sysr == null)
				return;
			if (Nebula != null && nr != Nebula)
				return;
			var dsq = VectorMath.DistanceSquared(pos, camera.Position);
			if (LODRanges != null) //Fastest cull
			{
				var maxd = LODRanges[LODRanges.Length - 1] * sysr.LODMultiplier;
				maxd *= maxd;
				if (dsq > maxd) return;
			}
			if (Dfm != null)
			{
				Dfm.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				var center = VectorMath.Transform(Vector3.Zero, World);
				//var lighting = RenderHelpers.ApplyLights(lights, LightGroup, center, 20, nr, LitAmbient, LitDynamic, NoFog);
				Dfm.DrawBuffer(commands, World, Lighting.Empty);
			}
			if (Model != null) {
				if (Model.Levels.Length > 0) {
					Model.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
					var center = VectorMath.Transform(Model.Levels[0].Center, World);
					var lvl = GetLevel(Model, center, camera.Position);
					if (lvl == null) return;
					var bsphere = new BoundingSphere(
						center,
						Model.Levels[0].Radius
					);
					if (camera.Frustum.Intersects(bsphere)) {
						var lighting = RenderHelpers.ApplyLights(lights, LightGroup, center, Model.Levels[0].Radius, nr, LitAmbient, LitDynamic, NoFog);
						var r = Model.Levels [0].Radius + lighting.FogRange.Y;
						if (lighting.FogMode != FogModes.Linear || VectorMath.DistanceSquared(camera.Position, center) <= (r * r))
							Model.DrawBufferLevel(lvl, commands, World, lighting);
					}
				}
			} else if (Cmp != null) {
				Cmp.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				foreach (Part p in Cmp.Parts.Values)
				{
					var model = p.Model;
					Matrix4 w = World;
					if (p.Construct != null)
						w = p.Construct.Transform * World;
					if (model.Levels.Length > 0)
					{

						var center = VectorMath.Transform(model.Levels[0].Center, w);
						var lvl = GetLevel(model, center, camera.Position);
						if (lvl == null) continue;
						var bsphere = new BoundingSphere(
							center,
							model.Levels[0].Radius
						);
						if (camera.Frustum.Intersects(bsphere))
						{
							var lighting = RenderHelpers.ApplyLights(lights, LightGroup, center, model.Levels[0].Radius, nr, LitAmbient, LitDynamic, NoFog);
							var r = model.Levels [0].Radius + lighting.FogRange.Y;
							if (lighting.FogMode != FogModes.Linear || VectorMath.DistanceSquared(camera.Position, center) <= (r * r))
								model.DrawBufferLevel(lvl, commands, w, lighting);
						}
					}
				}
			} else if (CmpParts != null) {
				_parentCmp.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				foreach (Part p in CmpParts)
				{
					p.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
					var model = p.Model;
					Matrix4 w = World;
					if (p.Construct != null)
						w = p.Construct.Transform * World;
					if (model.Levels.Length > 0)
					{
						var center = VectorMath.Transform(model.Levels[0].Center, w);
						var lvl = GetLevel(model, center, camera.Position);
						if (lvl == null) continue;
						var bsphere = new BoundingSphere(
							center,
							model.Levels[0].Radius
						);
						if (camera.Frustum.Intersects(bsphere))
						{
							var lighting = RenderHelpers.ApplyLights(lights, LightGroup, center, model.Levels[0].Radius, nr, LitAmbient, LitDynamic, NoFog);
							var r = model.Levels[0].Radius + lighting.FogRange.Y;
							if (lighting.FogMode != FogModes.Linear || VectorMath.DistanceSquared(camera.Position, center) <= (r * r))
								model.DrawBufferLevel(lvl, commands, w, lighting);
						}
					}
				}
			} else if (Sph != null) {
				Sph.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				var bsphere = new BoundingSphere(
					pos,
					radiusAtmosphere);
				if (camera.Frustum.Intersects(bsphere))
				{
					var l = RenderHelpers.ApplyLights(lights, LightGroup, pos, Sph.Radius, nr, LitAmbient, LitDynamic, NoFog);
					var r = Sph.Radius + l.FogRange.Y;
					if(l.FogMode != FogModes.Linear || VectorMath.DistanceSquared(camera.Position, pos) <= (r * r))
						Sph.DrawBuffer(commands, World, l);
				}
			}
		}
	}
}

