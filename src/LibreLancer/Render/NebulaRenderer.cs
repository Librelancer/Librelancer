// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.GameData;
using LibreLancer.Utf.Cmp;
using LibreLancer.Vertices;

namespace LibreLancer
{
	public class NebulaRenderer
	{
		public Nebula Nebula;
		Random rand;
		ICamera camera;
        Game game;
        NebulaVertices nverts;
        Renderer2D render2D;
        ResourceManager resman;
        Billboards billboards;
		List<ExteriorPuff> Exterior = new List<ExteriorPuff>();
		public NebulaRenderer(Nebula n, ICamera c, Game g)
		{
			Nebula = n;
			camera = c;
			game = g;
            nverts = g.GetService<NebulaVertices>();
            render2D = g.GetService<Renderer2D>();
            resman = g.GetService<ResourceManager>();
            billboards = g.GetService<Billboards>();
			rand = new Random();
			if (n.HasInteriorClouds)
			{
				puffsinterior = new InteriorPuff[n.InteriorCloudCount];
				for (int i = 0; i < n.InteriorCloudCount; i++)
					puffsinterior[i].Spawned = false;
			}
			GenerateExteriorPuffs();
			//Set Timers
			dynLightningTimer = Nebula.DynamicLightningGap;
			bckLightningTimer = Nebula.BackgroundLightningGap;
		}

		public bool FogTransitioned()
		{
			if (Math.Abs(Nebula.Zone.EdgeFraction) < 0.000000001) //basically == 0. Instant transition
				return true;
			var scaled = Nebula.Zone.Shape.Scale(1 - Nebula.Zone.EdgeFraction);
			return scaled.ContainsPoint(camera.Position);
		}

		float CalculateTransition(Zone zone)
		{
			//Find transitional value based on how far into the zone we are
			//ScaledDistance is from 0 at center to 1 at edge. Reverse this.
			var sd = 1 - MathHelper.Clamp(zone.Shape.ScaledDistance(camera.Position), 0f, 1f);
			return MathHelper.Clamp(sd / zone.EdgeFraction, 0, 1);
		}

		public void RenderFogTransition()
		{
			var c = GetFogColor();
			c.A = CalculateTransition(Nebula.Zone);

			render2D.Start(game.Width, game.Height);
			render2D.FillRectangle(new Rectangle(0, 0, game.Width, game.Height), c);
			render2D.Finish();
		}

		Color4 GetFogColor()
		{
			if (bckLightningActive)
				return Nebula.BackgroundLightningColor;
			else
				return Nebula.FogColor;
		}

		public void Update(TimeSpan elapsed)
		{
			if (Nebula.Zone.Shape.ContainsPoint(camera.Position))
			{
				UpdateBackgroundLightning(elapsed);
				UpdateDynamicLightning(elapsed);
				UpdateCloudLightning(elapsed);
				if (Nebula.HasInteriorClouds)
				{
					for (int i = 0; i < Nebula.InteriorCloudCount; i++)
					{
						if (!puffsinterior[i].Spawned ||
							VectorMath.Distance(puffsinterior[i].Position, camera.Position) > Nebula.InteriorCloudMaxDistance)
						{
							puffsinterior[i].Color = GetPuffColor();
							puffsinterior[i].Shape = Nebula.InteriorCloudShapes.GetNext();
							puffsinterior[i].Position = camera.Position + RandomPointSphere(Nebula.InteriorCloudMaxDistance);
							puffsinterior[i].Spawned = true;
							puffsinterior[i].Velocity = RandomDirection() * Nebula.InteriorCloudDrift;
						}
						puffsinterior[i].Position += puffsinterior[i].Velocity * (float)elapsed.TotalSeconds;
					}
				}
			}
		}

		double bckLightningTimer = 0.0;
		bool bckLightningActive = false;
		void UpdateBackgroundLightning(TimeSpan elapsed)
		{
            //Vanilla St03 has lightning but with 0 duration
			if (!Nebula.BackgroundLightning || Nebula.BackgroundLightningDuration == 0)
				return;
			bckLightningTimer -= elapsed.TotalSeconds;
			if (bckLightningActive == false)
			{
				if (bckLightningTimer <= 0.0)
				{
					bckLightningActive = true;
					bckLightningTimer = Nebula.BackgroundLightningDuration;
				}
			}
			else
			{
				if (bckLightningTimer <= 0.0)
				{
					bckLightningActive = false;
					bckLightningTimer = Nebula.BackgroundLightningGap;
				}
			}
		}

		double dynLightningTimer = 0.0;
		bool dynLightningActive = false;
		Vector3 dynamicLightningPos = Vector3.Zero;
		void UpdateDynamicLightning(TimeSpan elapsed)
		{
			if (!Nebula.DynamicLightning)
				return;
			dynLightningTimer -= elapsed.TotalSeconds;
			if (dynLightningActive == false)
			{
				if (dynLightningTimer <= 0.0)
				{
					dynLightningActive = true;
					dynLightningTimer = Nebula.DynamicLightningDuration;
					//spawn dynamic lightning
					dynamicLightningPos = camera.Position + RandomPointSphere(Nebula.FogRange.Y);
				}
			}
			else
			{
				if (dynLightningTimer <= 0.0)
				{
					dynLightningActive = false;
					dynLightningTimer = Nebula.DynamicLightningGap;
				}
			}
		}

		double cldLightningTimer = 0.0;
		bool cldLightningActive = false;
		void UpdateCloudLightning(TimeSpan elapsed)
		{
			if (!Nebula.CloudLightning)
				return;
			cldLightningTimer -= elapsed.TotalSeconds;
			if (cldLightningActive == false)
			{
				if (cldLightningTimer <= 0.0)
				{
					cldLightningActive = true;
					cldLightningTimer = Nebula.CloudLightningDuration;
				}
			}
			else
			{
				if (cldLightningTimer <= 0.0)
				{
					cldLightningActive = false;
					cldLightningTimer = Nebula.CloudLightningGap;
				}
			}
		}

		public void GetLighting(out bool FogEnabled, out Color4? Ambient, out Vector2 FogRange, out Color4 FogColor, out RenderLight? lightning)
		{
			Ambient = Nebula.AmbientColor;
			FogEnabled = Nebula.FogEnabled;
			FogColor = GetFogColor();
			FogRange = Nebula.FogRange;
			var ex = GetExclusion(camera.Position);
			if (ex != null)
			{
				var factor = CalculateTransition(ex.Zone);
				FogRange.Y = MathHelper.Lerp(FogRange.Y, ex.FogFar, factor);;
			}
			lightning = null;
			if (dynLightningActive)
			{
				var rl = new RenderLight();
				rl.Kind = LightKind.Point;
                rl.Color = new Color3f(Nebula.DynamicLightningColor.R,
                    Nebula.DynamicLightningColor.G,
                    Nebula.DynamicLightningColor.B);
				rl.Position = dynamicLightningPos;
				rl.Attenuation = new Vector3(1, 0, 0.0000055f);
				rl.Range = (int)Nebula.FogRange.Y;
				lightning = rl;
			}
		}

		public bool DoLightning(out PointLight lt)
		{
			lt = new PointLight();
			if (dynLightningActive)
			{
				lt.Position = new Vector4(dynamicLightningPos, 1);
				lt.ColorRange = new Vector4(
					Nebula.DynamicLightningColor.R,
					Nebula.DynamicLightningColor.G,
					Nebula.DynamicLightningColor.B,
					Nebula.FogRange.Y
				);
				lt.Attenuation = new Vector4(1, 0, 0.0000055f, 0);
				return true;
			}
			return false;
		}

		ExclusionZone GetExclusion(Vector3 position)
		{
			if (Nebula.ExclusionZones != null)
			{
				foreach (var zone in Nebula.ExclusionZones)
					if (zone.Zone.Shape.ContainsPoint(position))
						return zone;
			}
			return null;
		}

		public void Draw(CommandBuffer buffer)
		{
			bool inside = Nebula.Zone.Shape.ContainsPoint(camera.Position);
			if (!inside || !FogTransitioned())
				RenderFill(buffer, inside);
			DrawPuffRing(inside);
			if (inside)
			{
				var ex = GetExclusion(camera.Position);
				if (ex != null)
				{
					RenderExclusionZone(buffer, ex);
				}
				else
					RenderInteriorPuffs();
			}
		}

		void RenderExclusionZone(CommandBuffer buffer, ExclusionZone ex)
		{
			if (ex.Shell == null)
				return;
			Vector3 sz = Vector3.Zero;
			//Only render ellipsoid and sphere exteriors
			if (Nebula.Zone.Shape is ZoneEllipsoid)
				sz = ((ZoneEllipsoid)Nebula.Zone.Shape).Size / 2; //we want radius instead of diameter
			else if (Nebula.Zone.Shape is ZoneSphere)
				sz = new Vector3(((ZoneSphere)Nebula.Zone.Shape).Radius);
			else
				return;
			sz *= (1 / ex.Shell.GetRadius());
			var world = Matrix4.CreateScale(ex.ShellScalar * sz) * ex.Zone.RotationMatrix * Matrix4.CreateTranslation(ex.Zone.Position);
			var shell = (ModelFile)ex.Shell;
			//Calculate Alpha
			var alpha = ex.ShellMaxAlpha * CalculateTransition(ex.Zone);
			//Set all render materials. We don't want LOD for this Mesh.
			var l0 = shell.Levels[0];
			for (int i = l0.StartMesh; (i < l0.StartMesh + l0.MeshCount); i++)
			{
				var mat = (BasicMaterial)l0.Mesh.Meshes[i].Material.Render;
				mat.Oc = alpha;
				mat.OcEnabled = true;
				mat.AlphaEnabled = true;
				mat.Dc = new Color4(ex.ShellTint, 1) * (Nebula.AmbientColor ?? Color4.White);
			}
			//Render
			l0.Update(camera, TimeSpan.Zero);
			l0.DrawBuffer(buffer, world, ref Lighting.Empty, null);
		}
        static Shader _puffringsh;
		static int _ptex0;
		static int _pfogcolor;
		static int _pfogfactor;
        Shader GetPuffShader(Billboards bl)
        {
			if (_puffringsh == null)
			{
				_puffringsh = bl.GetShader("nebula_extpuff.frag");
				_ptex0 = _puffringsh.GetLocation("tex0");
				_pfogcolor = _puffringsh.GetLocation("FogColor");
				_pfogfactor = _puffringsh.GetLocation("FogFactor");
			}
            return _puffringsh;
        }
		void DrawPuffRing(bool inside)
		{
            /* Skip rendering puff rings */
            if(!inside) {
                Vector3 sz;
                if (Nebula.Zone.Shape is ZoneEllipsoid)
                    sz = ((ZoneEllipsoid)Nebula.Zone.Shape).Size / 2; //we want radius instead of diameter
                else if (Nebula.Zone.Shape is ZoneSphere)
                    sz = new Vector3(((ZoneSphere)Nebula.Zone.Shape).Radius);
                else
                    return;
                var bitRadius = Nebula.ExteriorBitRadius * (1 + Nebula.ExteriorBitRandomVariation);
                var szR = Math.Max(sz.X, Math.Max(sz.Y, sz.Z));
                var sph = new BoundingSphere(Nebula.Zone.Position, (szR + bitRadius) * 1.2f);
                if (camera.Frustum.Contains(sph) == ContainmentType.Disjoint)
                    return;
            }
            /* Actually Render */
			var sd = 1 - MathHelper.Clamp(Nebula.Zone.Shape.ScaledDistance(camera.Position), 0f, 1f);
			var factor = MathHelper.Clamp(sd / Nebula.Zone.EdgeFraction, 0, 1);

			for (int i = 0; i < Exterior.Count; i++)
			{
				var p = Exterior[i];
				var tex = resman.FindTexture(p.Shape.Texture);
				billboards.DrawCustomShader(
					GetPuffShader(billboards),
					_setupPuffDelegate,
					new RenderUserData() { Texture = tex, Color = Nebula.FogColor, Float = factor },
					p.Position,
					p.Size,
					Nebula.ExteriorColor,
					new Vector2(p.Shape.Dimensions.X, p.Shape.Dimensions.Y),
					new Vector2(p.Shape.Dimensions.X + p.Shape.Dimensions.Width, p.Shape.Dimensions.Y),
					new Vector2(p.Shape.Dimensions.X, p.Shape.Dimensions.Y + p.Shape.Dimensions.Height),
					new Vector2(p.Shape.Dimensions.X + p.Shape.Dimensions.Width, p.Shape.Dimensions.Y + p.Shape.Dimensions.Height),
					0,
					inside ? SortLayers.NEBULA_INSIDE : SortLayers.NEBULA_NORMAL
				);
			}
		}

		static ShaderAction _setupPuffDelegate = SetupPuffShader;
		static void SetupPuffShader(Shader sh, RenderState rs, ref RenderCommand dat)
		{
			sh.SetInteger(_ptex0, 0);
			dat.UserData.Texture.BindTo(0);
			sh.SetColor4(_pfogcolor, dat.UserData.Color);
			sh.SetFloat(_pfogfactor, dat.UserData.Float);
		}

		void GenerateExteriorPuffs()
		{
			var rn = new Random(1001);
			GeneratePuffRing(0.25f , rn);
			GeneratePuffRing(0.5f, rn);
			GeneratePuffRing(0.75f, rn);
		}

		void GeneratePuffRing(float ypct, Random rn)
		{
			Vector3 sz = Vector3.Zero;
			//Only render ellipsoid and sphere exteriors
			if (Nebula.Zone.Shape is ZoneEllipsoid)
				sz = ((ZoneEllipsoid)Nebula.Zone.Shape).Size / 2; //we want radius instead of diameter
			else if (Nebula.Zone.Shape is ZoneSphere)
				sz = new Vector3(((ZoneSphere)Nebula.Zone.Shape).Radius);
			else
				return;
			var yval = ypct * sz.Y;
			int puffcount = rn.Next(Nebula.ExteriorMinBits, Nebula.ExteriorMaxBits + 1);
			double current_angle = 0;
			double delta_angle = (2 * Math.PI) / puffcount;
			for (int i = 0; i < puffcount; i++)
			{
				var puff = new ExteriorPuff();
				var y = rn.NextFloat(
					yval - (sz.Y * Nebula.ExteriorMoveBitPercent),
					yval + (sz.Y * Nebula.ExteriorMoveBitPercent)
				);
				var pos = PrimitiveMath.GetPointOnRadius(sz, y, (float)current_angle);
				puff.Position = Nebula.Zone.Position + new Vector3(pos.X, pos.Y - (sz.Y / 2), pos.Z);
				var radius = rn.NextFloat(
					Nebula.ExteriorBitRadius * (1 - Nebula.ExteriorBitRandomVariation),
					Nebula.ExteriorBitRadius * (1 + Nebula.ExteriorBitRandomVariation)
				);
				puff.Size = new Vector2(radius * 2);
				puff.Shape = Nebula.ExteriorCloudShapes.GetNext();
				Exterior.Add(puff);
				current_angle += delta_angle;
			}
		}
		struct ExteriorPuff
		{
			public Vector3 Position;
			public Vector2 Size;
			public CloudShape Shape;
		}

		struct InteriorPuff
		{
			public bool Spawned;
			public Vector3 Position;
			public Vector3 Velocity;
			public Color3f Color;
			public CloudShape Shape;
		}

		InteriorPuff[] puffsinterior;
		void RenderInteriorPuffs()
		{
			if (Nebula.HasInteriorClouds)
			{
				for (int i = 0; i < Nebula.InteriorCloudCount; i++)
				{
					if (!puffsinterior[i].Spawned)
						continue;
					var distance = VectorMath.Distance(puffsinterior[i].Position, camera.Position);
					var alpha = Nebula.InteriorCloudMaxAlpha;
					if (distance > Nebula.InteriorCloudFadeDistance.X && distance < Nebula.InteriorCloudFadeDistance.Y)
					{
						var distance_difference = Nebula.InteriorCloudFadeDistance.Y - Nebula.InteriorCloudFadeDistance.X;
						var current = distance - Nebula.InteriorCloudFadeDistance.X;
						alpha -= (alpha * (distance_difference - current) / distance_difference);
					}
					if (distance < Nebula.InteriorCloudFadeDistance.X)
						alpha = 0;
					var shape = puffsinterior[i].Shape;
					Color4 c = new Color4(puffsinterior[i].Color, alpha);
					if (cldLightningActive)
					{
						c *= Nebula.CloudLightningColor;
					}
					billboards.Draw(
						(Texture2D)resman.FindTexture(shape.Texture),
						puffsinterior[i].Position,
						new Vector2(Nebula.InteriorCloudRadius),
						c,
						new Vector2(shape.Dimensions.X, shape.Dimensions.Y),
						new Vector2(shape.Dimensions.X + shape.Dimensions.Width, shape.Dimensions.Y),
						new Vector2(shape.Dimensions.X, shape.Dimensions.Y + shape.Dimensions.Height),
						new Vector2(shape.Dimensions.X + shape.Dimensions.Width, shape.Dimensions.Y + shape.Dimensions.Height),
						0,
						SortLayers.OBJECT
					);
				}
			}
		}
							                                                 
		Vector3 RandomDirection()
		{
			var v = new Vector3(
				-1f + (float)(rand.NextDouble() * 2),
				-1f + (float)(rand.NextDouble() * 2),
				-1f + (float)(rand.NextDouble() * 2)
			);
			v.Normalize();
			return v;
		}
							                                                 
		Vector3 RandomPointSphere(float radius)
		{
			var phi = (rand.NextDouble() * (2 * Math.PI));
			var costheta = (-1 + (rand.NextDouble() * 2));
			var u = rand.NextDouble();

			var theta = Math.Acos(costheta);
			var r = radius * Math.Pow(u, 1.0 / 3.0);

			var x = r * Math.Sin(theta) * Math.Cos(phi);
			var y = r * Math.Sin(theta) * Math.Sin(phi);
			var z = r * Math.Cos(theta);
			return new Vector3((float)x, (float)y, (float)z);
		}

		Color3f GetPuffColor()
		{
			var lerpval = rand.NextDouble();
			var c = Utf.Ale.AlchemyEasing.EaseColorRGB(
				Utf.Ale.EasingTypes.Linear,
				(float)lerpval,
				0,
				1,
				Nebula.InteriorCloudColorA,
				Nebula.InteriorCloudColorB
			);
			return new Color3f(c.R, c.G, c.B);
		}

		void RenderFill(CommandBuffer buffer, bool inside)
		{
			if (Nebula.ExteriorFill == null) return;
			Vector3 sz = Vector3.Zero;
			//Only render ellipsoid and sphere exteriors
			if (Nebula.Zone.Shape is ZoneEllipsoid)
				sz = ((ZoneEllipsoid)Nebula.Zone.Shape).Size / 2; //we want radius instead of diameter
			else if (Nebula.Zone.Shape is ZoneSphere)
				sz = new Vector3(((ZoneSphere)Nebula.Zone.Shape).Radius);
			else
				return;
			var p = Nebula.Zone.Position;
            var sph = new BoundingSphere(p, Math.Max(sz.X, Math.Max(sz.Y, sz.Z)) * 1.2f);
            if (camera.Frustum.Contains(sph) == ContainmentType.Disjoint)
                return;

			var tex = (Texture2D)resman.FindTexture(Nebula.ExteriorFill);
			//X axis
			{
				var tl = new VertexPositionTexture(
					new Vector3(-1, -1, 0),
					new Vector2(0, 1)
				);
				var tr = new VertexPositionTexture(
					new Vector3(+1, -1, 0),
					new Vector2(1, 1)
				);
				var bl = new VertexPositionTexture(
					new Vector3(-1, +1, 0),
					new Vector2(0, 0)
				);
				var br = new VertexPositionTexture(
					new Vector3(+1, +1, 0),
					new Vector2(1, 0)
				);
				nverts.SubmitQuad(
					tl, tr, bl, br
				);
			}
			//Z Axis
			{
				var tl = new VertexPositionTexture(
					new Vector3(0, -1, -1),
					new Vector2(0, 1)
				);
				var tr = new VertexPositionTexture(
					new Vector3(0, -1, +1),
					new Vector2(1, 1)
				);
				var bl = new VertexPositionTexture(
					new Vector3(0, +1, -1),
					new Vector2(0, 0)
				);
				var br = new VertexPositionTexture(
					new Vector3(0, +1, +1),
					new Vector2(0, 1)
				);
				nverts.SubmitQuad(
					tl, tr, bl, br
				);
			}
			//Y Axis
			{
				var tl = new VertexPositionTexture(
					new Vector3(-1, 0, -1),
					new Vector2(0, 1)
				);
				var tr = new VertexPositionTexture(
					new Vector3(-1, 0, 1),
					new Vector2(1, 1)
				);
				var bl = new VertexPositionTexture(
					new Vector3(+1, 0, -1),
					new Vector2(0, 0)
				);
				var br = new VertexPositionTexture(
					new Vector3(+1, 0, 1),
					new Vector2(0, 1)
				);
				nverts.SubmitQuad(
					tl, tr, bl, br
				);
			}
			var transform = Matrix4.CreateScale(sz) * Nebula.Zone.RotationMatrix * Matrix4.CreateTranslation(p);
			nverts.Draw(
				buffer,
				camera,
				tex,
				Nebula.FogColor,
				transform,
				inside
			);
		}
	}
}

