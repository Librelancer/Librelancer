// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
			if (Nebula == null || pos != position && sysr != null)
			{
				pos = position;
				Nebula = sysr.ObjectInNebula(position);
			}
		}

        void Init()
        {
            if (!inited)
            {
                if (Dfm != null)
                    Dfm.Initialize(sysr.ResourceManager);
                if (Model != null && Model.Levels.Length > 0)
                    Model.Initialize(sysr.ResourceManager);
                else if (Cmp != null)
                    Cmp.Initialize(sysr.ResourceManager);
                else if (Sph != null)
                {
                    Sph.Initialize(sysr.ResourceManager);
                    if (Sph.SideMaterials.Length > 6)
                        radiusAtmosphere = Sph.Radius * Math.Max(Sph.SideMaterials[6].Scale,1f);
                    else
                        radiusAtmosphere = Sph.Radius;
                }
                inited = true;
            }
        }

		VMeshRef GetLevel(ModelFile file, Vector3 center, Vector3 camera)
		{
			float[] ranges = LODRanges ?? file.Switch2;
			var dsq = VectorMath.DistanceSquared(center, camera);
            if (ranges == null) {
                CurrentLevel = 0;
                return file.Levels[0];
            }
			var lvl = file.Levels[0];
			for (int i = 0; i < ranges.Length; i++)
			{
				var d = ranges[i];
				if (i > 0 && ranges[i] < ranges[i - 1]) break;
				if (dsq < (d * sysr.LODMultiplier) * (d * sysr.LODMultiplier)) break;
                if (i >= file.Levels.Length) {
                    CurrentLevel = -1;
                    return null;
                }
				lvl = file.Levels[i];
                CurrentLevel = i;
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
				var partCol = (IEnumerable<Part>)CmpParts ?? Cmp.Parts;
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
                    Math.Max(Sph.Radius,radiusAtmosphere));
				if (!camera.Frustum.Intersects(bsphere)) return true;
			}
			return false;
		}

		public override void DepthPrepass(ICamera camera, RenderState rstate)
		{
            Init();
			if (Model != null)
			{
				Model.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				if (Model.Levels.Length != 0)
				{
					var center = VectorMath.Transform(Model.Levels[0].Center, World);
					var lvl = GetLevel(Model, center, camera.Position);
					if (lvl == null) return;
					Model.DepthPrepassLevel(lvl, rstate, World);
				}
			}
			else if (Cmp != null || CmpParts != null)
			{
				if (Cmp != null) Cmp.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				else _parentCmp.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				//Check if -something- renders
				var partCol = (IEnumerable<Part>)CmpParts ?? Cmp.Parts;
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
							model.DepthPrepassLevel(lvl, rstate, w);
						}
					}
				}
			}
			else if (Sph != null)
			{
				Sph.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				Sph.DepthPrepass(rstate, World);
			}
		}

        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
		{
            this.sysr = sys;
			if (Nebula != null && nr != Nebula)
			{
                return false;
			}
			var dsq = VectorMath.DistanceSquared(pos, camera.Position);
			if (LODRanges != null) //Fastest cull
			{
				var maxd = LODRanges[LODRanges.Length - 1] * sysr.LODMultiplier;
				maxd *= maxd;
                if (dsq > maxd) return false;
			}
			if (Model != null)
			{
				if (Model.Levels.Length != 0)
				{
					var center = VectorMath.Transform(Model.Levels[0].Center, World);
					var lvl = GetLevel(Model, center, camera.Position);
                    if (lvl == null) return false;
					var bsphere = new BoundingSphere(
						center,
						Model.Levels[0].Radius
					);
                    if (!camera.Frustum.Intersects(bsphere)) return false; //Culled
                    sys.AddObject(this);
                    return true;
				}
			}
			else if (Cmp != null || CmpParts != null)
			{
				//Check if -something- renders
				var partCol = (IEnumerable<Part>)CmpParts ?? Cmp.Parts;
				bool cmpParts = CmpParts != null;
				foreach (Part p in partCol)
				{
					if(cmpParts) p.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
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
                            sys.AddObject(this);
                            return true;
						}
					}
				}
                return false;
			}
			else if (Sph != null)
			{
				var bsphere = new BoundingSphere(
					pos,
                    Math.Max(Sph.Radius, radiusAtmosphere));
                if (!camera.Frustum.Intersects(bsphere)) return false;
                sys.AddObject(this);
                return true;
			}
            return false;
		}
		public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
            Init();
			if (Dfm != null)
			{
				Dfm.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				var center = VectorMath.Transform(Vector3.Zero, World);
				//var lighting = RenderHelpers.ApplyLights(lights, LightGroup, center, 20, nr, LitAmbient, LitDynamic, NoFog);
				Dfm.DrawBuffer(commands, World, ref Lighting.Empty);
			}
			if (Model != null) {
				if (Model.Levels.Length > 0) {
					Model.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
					var center = VectorMath.Transform(Model.Levels[0].Center, World);
					var lvl = GetLevel(Model, center, camera.Position);
					if (lvl == null) return;
					var lighting = RenderHelpers.ApplyLights(lights, LightGroup, center, Model.Levels[0].Radius, nr, LitAmbient, LitDynamic, NoFog);
					var r = Model.Levels [0].Radius + lighting.FogRange.Y;
					if (lighting.FogMode != FogModes.Linear || VectorMath.DistanceSquared(camera.Position, center) <= (r * r))
						Model.DrawBufferLevel(lvl, commands, World, ref lighting);
				}
			} else if (Cmp != null) {
				Cmp.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				foreach (Part p in Cmp.Parts)
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
								model.DrawBufferLevel(lvl, commands, w, ref lighting);
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
								model.DrawBufferLevel(lvl, commands, w, ref lighting);
						}
					}
				}
			} else if (Sph != null) {
				Sph.Update(camera, TimeSpan.Zero, TimeSpan.FromSeconds(sysr.Game.TotalTime));
				var l = RenderHelpers.ApplyLights(lights, LightGroup, pos, Sph.Radius, nr, LitAmbient, LitDynamic, NoFog);
				var r = Sph.Radius + l.FogRange.Y;
				if(l.FogMode != FogModes.Linear || VectorMath.DistanceSquared(camera.Position, pos) <= (r * r))
					Sph.DrawBuffer(commands, World, ref l);
			}
		}
	}
}

