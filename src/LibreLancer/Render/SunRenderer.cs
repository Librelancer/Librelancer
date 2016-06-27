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
	public class SunRenderer
	{
		public Sun Sun { get; private set; }
		FreelancerGame game;
		Vector3 pos;
		Vector3 camera_pos;
		public SunRenderer (SystemObject obj, FreelancerGame game)
		{
			Sun = obj.Archetype as Sun;
			pos = obj.Position;
			this.game = game;
		}
		public void Update (TimeSpan elapsed, ICamera camera)
		{
			camera_pos = camera.Position;
		}
		public void Draw ()
		{
			var dist_scale = 1; // TODO: Modify this based on nebula burn-through.
			var glow_scale = dist_scale * Sun.GlowScale;
			DrawRadial(
				(Texture2D)game.ResourceManager.FindTexture(Sun.GlowSprite),
				new Vector3(pos),
				new Vector2(Sun.Radius, Sun.Radius) * glow_scale,
				Sun.GlowColorInner,
				Sun.GlowColorOuter,
				0
			);
			if (Sun.CenterSprite != null)
			{
				var center_scale = dist_scale * Sun.CenterScale;
				DrawRadial(
					(Texture2D)game.ResourceManager.FindTexture(Sun.CenterSprite),
					new Vector3(pos),
					new Vector2(Sun.Radius, Sun.Radius) * center_scale,
					Sun.CenterColorInner,
					Sun.CenterColorOuter,
					0
				);
			}
		}
		void DrawRadial(Texture2D texture, Vector3 position, Vector2 size, Color4 inner, Color4 outer, float expand)
		{
			game.Billboards.DrawCustomShader(
				"sun_radial.frag",
				new RenderUserData() { Texture = texture, Color = inner, Color2 = outer, Float = expand,  UserFunction = _setupRadialDelegate },
				position,
				size,
				Color4.White,
				new Vector2(0, 0),
				new Vector2(0, 1),
				new Vector2(1, 0),
				new Vector2(1, 1),
				0
			);
		}
		static Action<Shader, RenderUserData> _setupRadialDelegate = SetupRadialShader;
		static void SetupRadialShader(Shader sh, RenderUserData dat)
		{
			sh.SetInteger("tex0", 0);
			dat.Texture.BindTo(0);
			sh.SetColor4("innercolor", dat.Color);
			sh.SetColor4("outercolor", dat.Color2);
			sh.SetFloat("expand", dat.Float);
		}
	}
}

