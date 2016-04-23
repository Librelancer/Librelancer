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
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using LibreLancer.Primitives;
using LibreLancer.Vertices;
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{
	public class DemoSystemView : GameState
	{
		const string DEMO_TEXT =
@"SYSTEM VIEWER DEMO
Controls:
WSAD - Move
Arrow Keys - Rotate Camera
Escape - Exit
Position: (X: {0:0.00}, Y: {1:0.00}, Z: {2:0.00})
";
		private const float ROTATION_SPEED = 1f;
		GameData.StarSystem sys;
		DebugCamera camera;
		SystemRenderer sysrender;
		bool wireframe = false;
		Renderer2D trender;
		Font font;
		public DemoSystemView (FreelancerGame g) : base(g)
		{
			FLLog.Info ("Game", "Starting System Viewer Demo");
			sys = g.GameData.GetSystem ("Li01");
			camera = new DebugCamera (g.Viewport);
			camera.Zoom = 5000;
			sysrender = new SystemRenderer (camera, g.GameData, g.ResourceManager);
			sysrender.StarSystem = sys;
			camera.UpdateProjection ();
			Game.KeyPress += (object sender, OpenTK.KeyPressEventArgs e) => {
				if(e.KeyChar == 'p') {
					wireframe = !wireframe;
					if(wireframe) {
						GL.PolygonMode (MaterialFace.FrontAndBack, PolygonMode.Line);
					} else {
						GL.PolygonMode (MaterialFace.FrontAndBack, PolygonMode.Fill);
					}
				}
			};
			trender = new Renderer2D (Game.RenderState);
			font = Font.FromSystemFont (trender, "Agency FB", 16);

		}

		public override void Update (TimeSpan delta)
		{
			if (Game.Keyboard [Key.Right]) {
				camera.Rotation = new Vector2 (camera.Rotation.X - (ROTATION_SPEED * (float)delta.TotalSeconds),
					camera.Rotation.Y);
			}
			if (Game.Keyboard [Key.Left]) {
				camera.Rotation = new Vector2 (camera.Rotation.X + (ROTATION_SPEED * (float)delta.TotalSeconds),
					camera.Rotation.Y);
			}
			if (Game.Keyboard [Key.Up]) {
				camera.Rotation = new Vector2 (camera.Rotation.X,
					camera.Rotation.Y  + (ROTATION_SPEED * (float)delta.TotalSeconds));
			}
			if (Game.Keyboard [Key.Down]) {
				camera.Rotation = new Vector2 (camera.Rotation.X,
					camera.Rotation.Y  - (ROTATION_SPEED * (float)delta.TotalSeconds));
			}
			if (Game.Keyboard [Key.W]) {
				camera.MoveVector = VectorMath.Forward;
			}
			if (Game.Keyboard [Key.S]) {
				camera.MoveVector = VectorMath.Backward;
			}
			if (Game.Keyboard [Key.A]) {
				camera.MoveVector = VectorMath.Left;
			}
			if (Game.Keyboard [Key.D]) {
				camera.MoveVector = VectorMath.Right;
			}
			if (Game.Keyboard [Key.Escape]) {
				Game.Exit ();
			}
			camera.Update (delta);
			camera.Free = true;
			sysrender.Update (delta);
		}
		public override void Draw (TimeSpan delta)
		{
			sysrender.Draw ();
			trender.Start (Game.Width, Game.Height);
			DrawShadowedText (string.Format(DEMO_TEXT,camera.Position.X, camera.Position.Y, camera.Position.Z), 5, 5);
			trender.Finish ();
		}

		void DrawShadowedText(string text, float x, float y)
		{
			trender.DrawString (font,
				text,
				x + 2, y + 2,
				Color4.Black);
			trender.DrawString (font,
				text,
				x, y,
				Color4.White);
		}
	}
}

