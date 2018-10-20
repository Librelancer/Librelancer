// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.GameData;
using LibreLancer.GameData.Archetypes;
namespace LibreLancer
{
	public class SunRenderer : ObjectRenderer
	{
		public Sun Sun { get; private set; }
		Vector3 pos;
		SystemRenderer sysr;
		public SunRenderer (Sun sun)
		{
			Sun = sun;
			pos = Vector3.Zero;
		}
		public override void Update(TimeSpan elapsed, Vector3 position, Matrix4 transform)
		{
			pos = position;
		}

        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            sysr = sys;
            sys.AddObject(this);
            return true;
        }
		public override void Draw (ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
			if (sysr == null)
				return;
			float z = RenderHelpers.GetZ(Matrix4.Identity, camera.Position, pos);
			if (z > 900000) // Reduce artefacts from fast Z-sort calculation. This'll probably cause issues somewhere else
				z = 900000;
			var dist_scale = nr != null ? nr.Nebula.SunBurnthroughScale : 1; // TODO: Modify this based on nebula burn-through.
			var alpha = nr != null ? nr.Nebula.SunBurnthroughIntensity : 1;
			var glow_scale = dist_scale * Sun.GlowScale;
			if (Sun.CenterSprite != null)
			{
				var center_scale = dist_scale * Sun.CenterScale;
				DrawRadial(
					(Texture2D)sysr.ResourceManager.FindTexture(Sun.CenterSprite),
					new Vector3(pos),
					new Vector2(Sun.Radius, Sun.Radius) * center_scale,
					new Color4(Sun.CenterColorInner, 1),
					new Color4(Sun.CenterColorOuter, alpha),
					0,
					z
				);
			}
			DrawRadial(
				(Texture2D)sysr.ResourceManager.FindTexture(Sun.GlowSprite),
				new Vector3(pos),
				new Vector2(Sun.Radius, Sun.Radius) * glow_scale,
				new Color4(Sun.GlowColorInner, 0),
				new Color4(Sun.GlowColorOuter, alpha),
				0,
				z + 108f
			);
			if (Sun.SpinesSprite != null && nr == null)
			{
				double current_angle = 0;
				double delta_angle = (2 * Math.PI) / Sun.Spines.Count;
				var spinetex = (Texture2D)sysr.ResourceManager.FindTexture(Sun.SpinesSprite);
				for (int i = 0; i < Sun.Spines.Count; i++)
				{
					var s = Sun.Spines[i];
					current_angle += delta_angle;
					DrawSpine(
						spinetex,
						pos,
						new Vector2(Sun.Radius, Sun.Radius) * Sun.SpinesScale * new Vector2(s.WidthScale / s.LengthScale, s.LengthScale),
						s.InnerColor,
						s.OuterColor,
						s.Alpha,
						(float)current_angle,
						z + 1112f + (2f * i)
					);
				}
			}
		}
		static int _spinetex0;
		static int _spineinner;
		static int _spineouter;
		static int _spinealpha;
        static Shader _spinesh;
        Shader GetSpineShader(Billboards bl)
        {
			if (_spinesh == null)
			{
				_spinesh = bl.GetShader("sun_spine.frag");
				_spinetex0 = _spinesh.GetLocation("tex0");
				_spineinner = _spinesh.GetLocation("innercolor");
				_spineouter = _spinesh.GetLocation("outercolor");
				_spinealpha = _spinesh.GetLocation("alpha");
			}
            return _spinesh;
        }
		static int _radialtex0;
		static int _radialinner;
		static int _radialouter;
		static int _radialexpand;
        static Shader _radialsh;
        Shader GetRadialShader(Billboards bl)
        {
			if (_radialsh == null)
			{
				_radialsh = bl.GetShader("sun_radial.frag");
				_radialtex0 = _radialsh.GetLocation("tex0");
				_radialinner = _radialsh.GetLocation("innercolor");
				_radialouter = _radialsh.GetLocation("outercolor");
				_radialexpand = _radialsh.GetLocation("expand");
			}
            return _radialsh;
        }
        void DrawRadial(Texture2D texture, Vector3 position, Vector2 size, Color4 inner, Color4 outer, float expand, float z)
		{
			sysr.Billboards.DrawCustomShader(
				GetRadialShader(sysr.Billboards),
				_setupRadialDelegate,
				new RenderUserData() { Texture = texture, Color = inner, Color2 = outer, Float = expand },
				position,
				size,
				Color4.White,
				new Vector2(0, 0),
				new Vector2(0, 1),
				new Vector2(1, 0),
				new Vector2(1, 1),
				0,
				SortLayers.SUN,
				z
			);
		}
		void DrawSpine(Texture2D texture, Vector3 position, Vector2 size, Color3f inner, Color3f outer, float alpha, float angle, float z)
		{
			sysr.Billboards.DrawCustomShader(
				GetSpineShader(sysr.Billboards),
				_setupSpineDelegate,
				new RenderUserData() { Texture = texture, Color = new Color4(inner,1), Color2 = new Color4(outer,1), Float = alpha },
				position,
				size,
				Color4.White,
				new Vector2(0, 0),
				new Vector2(0, 1),
				new Vector2(1, 0),
				new Vector2(1, 1),
				angle,
				SortLayers.SUN,
				z
			);
		}
		static ShaderAction _setupRadialDelegate = SetupRadialShader;
		static void SetupRadialShader(Shader sh, RenderState rs, ref RenderCommand dat)
		{
			sh.SetInteger(_radialtex0, 0);
			dat.UserData.Texture.BindTo(0);
			var inner = new Color4(dat.UserData.Color.R, dat.UserData.Color.G, dat.UserData.Color.B, dat.UserData.Color2.A);
			sh.SetColor4(_radialinner, inner);
			sh.SetColor4(_radialouter, dat.UserData.Color2);
			sh.SetFloat(_radialexpand, dat.UserData.Float);
			rs.BlendMode = dat.UserData.Color.A == 1 ? BlendMode.Additive : BlendMode.Normal;
		}
		static ShaderAction _setupSpineDelegate = SetupSpineShader;
		static void SetupSpineShader(Shader sh, RenderState rs, ref RenderCommand dat)
		{
			sh.SetInteger(_spinetex0, 0);
			dat.UserData.Texture.BindTo(0);
			sh.SetVector3(_spineinner, new Vector3(dat.UserData.Color.R, dat.UserData.Color.G, dat.UserData.Color.B));
			sh.SetVector3(_spineouter, new Vector3(dat.UserData.Color2.R, dat.UserData.Color2.G, dat.UserData.Color2.B));
			sh.SetFloat(_spinealpha, dat.UserData.Float);
			rs.BlendMode = BlendMode.Additive;
		}
	}
}

