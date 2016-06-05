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
using OpenTK;
using OpenTK.Graphics;
using LibreLancer.GameData;
using LibreLancer.Vertices;

namespace LibreLancer
{
	public class NebulaRenderer
	{
		public Nebula Nebula;
		Random rand;
		ICamera camera;
		FreelancerGame game;

		public NebulaRenderer (Nebula n, ICamera c, FreelancerGame g)
		{
			Nebula = n;
			camera = c;
			game = g;
			rand = new Random();
		}

		public bool FogTransitioned()
		{
			if (Math.Abs(Nebula.Zone.EdgeFraction) < 0.000000001) //basically == 0. Instant transition
				return true;
			var scaled = Nebula.Zone.Shape.Scale(1 - Nebula.Zone.EdgeFraction);
			return scaled.ContainsPoint(Nebula.Zone.Position, camera.Position);
		}

		public void RenderFogTransition()
		{
			//Find transitional colour based on how far into the nebula we are
			//ScaledDistance is from 0 at center to 1 at edge. Reverse this.
			var sd = 1 - Nebula.Zone.Shape.ScaledDistance(Nebula.Zone.Position, camera.Position);
			var alpha = sd / Nebula.Zone.EdgeFraction; //at 0 distance, 0 alpha, at EdgeFraction distance, full alpha
			var c = Nebula.FogColor;
			c.A = alpha;

			game.Renderer2D.Start(game.Width, game.Height);
			game.Renderer2D.FillRectangle(new Rectangle(0, 0, game.Width, game.Height), c);
			game.Renderer2D.Finish();
		}

		public void Update(TimeSpan elapsed)
		{

		}

		public void Draw(Lighting lights)
		{
			if(!Nebula.Zone.Shape.ContainsPoint(Nebula.Zone.Position, camera.Position) || !FogTransitioned())
				RenderFill();
			if (Nebula.Zone.Shape.ContainsPoint(Nebula.Zone.Position, camera.Position))
				RenderInteriorPuffs();
		}

		void RenderInteriorPuffs()
		{
			
		}

		Color4 GetPuffColor()
		{
			var lerpval = rand.NextDouble();
			var c = Utf.Ale.AlchemyEasing.EaseColor(
				Utf.Ale.EasingTypes.Linear,
				(float)lerpval,
				0,
				1,
				Nebula.InteriorCloudColorA,
				Nebula.InteriorCloudColorB
			);
			return new Color4(c.R, c.G, c.B, 1f);  
		}

		void RenderFill()
		{
			Vector3 sz = Vector3.Zero;
			//Only render ellipsoid and sphere exteriors
			if (Nebula.Zone.Shape is ZoneEllipsoid)
				sz = ((ZoneEllipsoid)Nebula.Zone.Shape).Size / 2; //we want radius instead of diameter
			else if (Nebula.Zone.Shape is ZoneSphere)
				sz = new Vector3(((ZoneSphere)Nebula.Zone.Shape).Radius);
			else
				return;
			var p = Nebula.Zone.Position;
			var tex = (Texture2D)game.ResourceManager.FindTexture(Nebula.ExteriorFill).Texture;
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
					new Vector3( -1, +1, 0),
					new Vector2(0, 0)
				);
				var br = new VertexPositionTexture(
					new Vector3(+1, +1, 0),
					new Vector2(1, 0)
				);
				game.Nebulae.SubmitQuad(
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
				game.Nebulae.SubmitQuad(
					tl, tr, bl, br
				);
			}
			//Y Axis
			{
				var tl = new VertexPositionTexture(
					new Vector3(- 1, 0, - 1),
					new Vector2(0, 1)
				);
				var tr = new VertexPositionTexture(
					new Vector3(- 1, 0, 1),
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
				game.Nebulae.SubmitQuad(
					tl, tr, bl, br
				);
			}
			var transform = Matrix4.CreateScale(sz) * Nebula.Zone.Rotation * Matrix4.CreateTranslation(p);
			game.Nebulae.Draw(
				game.RenderState, 
				camera, 
				tex,
				Nebula.ExteriorColor,
				transform
			);
		}
	}
}

