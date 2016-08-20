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
		public override void Register(SystemRenderer renderer)
		{
			sysr = renderer;
			sysr.Objects.Add(this);
		}
		public override void Unregister()
		{
			sysr.Objects.Remove(this);
			sysr = null;
		}
		public override void Draw (ICamera camera, CommandBuffer commands, Lighting lights, NebulaRenderer nr)
		{
			if (sysr == null)
				return;
			float z = RenderHelpers.GetZ(Matrix4.Identity, camera.Position, pos);
			var dist_scale = nr != null ? nr.Nebula.SunBurnthroughScale : 1; // TODO: Modify this based on nebula burn-through.
			var alpha = nr != null ? nr.Nebula.SunBurnthroughIntensity : 1;
			var glow_scale = dist_scale * Sun.GlowScale;
			if (Sun.CenterSprite != null)
			{
				var center_scale = dist_scale * Sun.CenterScale;
				DrawRadial(
					(Texture2D)sysr.Game.ResourceManager.FindTexture(Sun.CenterSprite),
					new Vector3(pos),
					new Vector2(Sun.Radius, Sun.Radius) * center_scale,
					new Color4(Sun.CenterColorInner, alpha),
					new Color4(Sun.CenterColorOuter, alpha),
					0,
					z
				);
			}
			DrawRadial(
				(Texture2D)sysr.Game.ResourceManager.FindTexture(Sun.GlowSprite),
				new Vector3(pos),
				new Vector2(Sun.Radius, Sun.Radius) * glow_scale,
				new Color4(Sun.GlowColorInner, alpha),
				new Color4(Sun.GlowColorOuter, alpha),
				0,
				z + 1f
			);
			if (Sun.SpinesSprite != null && nr == null)
			{
				double current_angle = 0;
				double delta_angle = (2 * Math.PI) / Sun.Spines.Count;
				var spinetex = (Texture2D)sysr.Game.ResourceManager.FindTexture(Sun.SpinesSprite);
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
						z + 2f + (1f * i)
					);
				}
			}
		}
        static Shader _spinesh;
        Shader GetSpineShader(Billboards bl)
        {
            if (_spinesh == null)
                _spinesh = bl.GetShader("sun_spine.frag");
            return _spinesh;
        }
        static Shader _radialsh;
        Shader GetRadialShader(Billboards bl)
        {
            if (_radialsh == null)
                _radialsh = bl.GetShader("sun_radial.frag");
            return _radialsh;
        }
        void DrawRadial(Texture2D texture, Vector3 position, Vector2 size, Color4 inner, Color4 outer, float expand, float z)
		{
			sysr.Game.Billboards.DrawCustomShader(
				GetRadialShader(sysr.Game.Billboards),
				new RenderUserData() { Texture = texture, Color = inner, Color2 = outer, Float = expand, UserFunction = _setupRadialDelegate },
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
			sysr.Game.Billboards.DrawCustomShader(
				GetSpineShader(sysr.Game.Billboards),
				new RenderUserData() { Texture = texture, Color = new Color4(inner,1), Color2 = new Color4(outer,1), Float = alpha, UserFunction = _setupSpineDelegate },
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
		static Action<Shader, RenderState, RenderUserData> _setupRadialDelegate = SetupRadialShader;
		static void SetupRadialShader(Shader sh, RenderState rs, RenderUserData dat)
		{
			sh.SetInteger("tex0", 0);
			dat.Texture.BindTo(0);
			sh.SetColor4("innercolor", dat.Color);
			sh.SetColor4("outercolor", dat.Color2);
			sh.SetFloat("expand", dat.Float);
			if (!((Texture2D)dat.Texture).WithAlpha)
				rs.BlendMode = BlendMode.Additive;
		}
		static Action<Shader, RenderState, RenderUserData> _setupSpineDelegate = SetupSpineShader;
		static void SetupSpineShader(Shader sh, RenderState rs, RenderUserData dat)
		{
			sh.SetInteger("tex0", 0);
			dat.Texture.BindTo(0);
			sh.SetVector3("innercolor", new Vector3(dat.Color.R, dat.Color.G, dat.Color.B));
			sh.SetVector3("outercolor", new Vector3(dat.Color2.R, dat.Color2.G, dat.Color2.B));
			sh.SetFloat("alpha", dat.Float);
		}
	}
}

