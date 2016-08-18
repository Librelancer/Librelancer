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
using LibreLancer.Primitives;
using LibreLancer.Vertices;
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{
	public class DemoSystemView : GameState
	{
		const string DEMO_TEXT =
@"SYSTEM VIEWER DEMO
{3} ({4})
Controls:
WSAD, Arrow Keys - Move/Rotate
Tab - Switch System
Position: (X: {0:0.00}, Y: {1:0.00}, Z: {2:0.00})
C# Memory Usage: {5}
";
		private const float ROTATION_SPEED = 1f;
		GameData.StarSystem sys;
		GameWorld world;
		DebugCamera camera;
		SystemRenderer sysrender;
		bool wireframe = false;
		Renderer2D trender;
		Font font;
		bool textEntry = false;
		string currentText = "";
		public DemoSystemView (FreelancerGame g) : base(g)
		{
			FLLog.Info ("Game", "Starting System Viewer Demo");
			sys = g.GameData.GetSystem ("li01");
			camera = new DebugCamera (g.Viewport);
			camera.Zoom = 5000;
			sysrender = new SystemRenderer (camera, g.GameData, g.ResourceManager);
			world = new GameWorld(sysrender);
			world.LoadSystem(sys, g.ResourceManager);
			g.Sound.PlayMusic(sys.MusicSpace);
			camera.UpdateProjection ();

			trender = new Renderer2D (Game.RenderState);
			font = Font.FromSystemFont (trender, "Agency FB", 16);
			g.Keyboard.KeyDown += G_Keyboard_KeyDown;
			g.Keyboard.TextInput += G_Keyboard_TextInput;
		}

		public override void Update (TimeSpan delta)
		{
			if (!textEntry)
				ProcessInput(delta);
			else
			{
				if (Game.Keyboard.IsKeyDown(Keys.Enter))
				{
					textEntry = false;
					Game.DisableTextInput();
					sys = Game.GameData.GetSystem(currentText.Trim());
					world.LoadSystem(sys, Game.ResourceManager);
					camera.Free = false;
					camera.Update(TimeSpan.FromSeconds(1));
					camera.Free = true;
					Game.Sound.PlayMusic(sys.MusicSpace);
				}
				if (Game.Keyboard.IsKeyDown(Keys.Escape))
					textEntry = false;
				
			}
			camera.Update (delta);
			camera.Free = true;
			world.Update (delta);
		}

		void G_Keyboard_KeyDown (KeyEventArgs e)
		{
			if (e.Key == Keys.Backspace && textEntry)
			{
				if (currentText.Length > 0)
				{
					currentText = currentText.Substring(0, currentText.Length - 1);
				}
			}
			if (e.Key == Keys.P && !textEntry)
			{
				Game.RenderState.Wireframe = !Game.RenderState.Wireframe;
			}
		}

		void G_Keyboard_TextInput (string text)
		{
			if (textEntry)
				currentText += text;
			
		}
		void ProcessInput(TimeSpan delta)
		{
			if (Game.Keyboard.IsKeyDown(Keys.Tab))
			{
				currentText = "";
				textEntry = true;
				Game.EnableTextInput();
				return;
			}
			if (Game.Keyboard.IsKeyDown(Keys.Right))
			{
				camera.Rotation = new Vector2(camera.Rotation.X - (ROTATION_SPEED * (float)delta.TotalSeconds),
					camera.Rotation.Y);
			}
			if (Game.Keyboard.IsKeyDown(Keys.Left))
			{
				camera.Rotation = new Vector2(camera.Rotation.X + (ROTATION_SPEED * (float)delta.TotalSeconds),
					camera.Rotation.Y);
			}
			if (Game.Keyboard.IsKeyDown(Keys.Up))
			{
				camera.Rotation = new Vector2(camera.Rotation.X,
					camera.Rotation.Y + (ROTATION_SPEED * (float)delta.TotalSeconds));
			}
			if (Game.Keyboard.IsKeyDown(Keys.Down))
			{
				camera.Rotation = new Vector2(camera.Rotation.X,
					camera.Rotation.Y - (ROTATION_SPEED * (float)delta.TotalSeconds));
			}
			if (Game.Keyboard.IsKeyDown(Keys.W))
			{
				camera.MoveVector = VectorMath.Forward;
			}
			if (Game.Keyboard.IsKeyDown(Keys.S))
			{
				camera.MoveVector = VectorMath.Backward;
			}
			if (Game.Keyboard.IsKeyDown(Keys.A))
			{
				camera.MoveVector = VectorMath.Left;
			}
			if (Game.Keyboard.IsKeyDown(Keys.D))
			{
				camera.MoveVector = VectorMath.Right;
			}
			if (Game.Keyboard.IsKeyDown(Keys.D1))
			{
				camera.MoveSpeed = 3000;
			}
			if (Game.Keyboard.IsKeyDown(Keys.D2))
			{
				camera.MoveSpeed = 300;
			}
			if (Game.Keyboard.IsKeyDown(Keys.D3))
			{
				camera.MoveSpeed = 90;
			}
		}

		public override void Draw (TimeSpan delta)
		{
			sysrender.Draw ();
			trender.Start (Game.Width, Game.Height);
			DrawShadowedText (string.Format(DEMO_TEXT,camera.Position.X, camera.Position.Y, camera.Position.Z, sys.Id, sys.Name, SizeSuffix(GC.GetTotalMemory(false))), 5, 5);
			if (textEntry)
			{
				DrawShadowedText("Change System (Esc to cancel): " + currentText, 5, 200);
			}
			trender.Finish ();
		}

		static readonly string[] SizeSuffixes =
				   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		static string SizeSuffix(Int64 value)
		{
			if (value < 0) { return "-" + SizeSuffix(-value); }
			if (value == 0) { return "0.0 bytes"; }

			int mag = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
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

