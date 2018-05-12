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
@"SYSTEM VIEWER DEMO - {6}
{3} ({4})
Controls:
WSAD, Arrow Keys - Move/Rotate
Tab - Switch System
L - Screenshot
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
			sysrender = new SystemRenderer (camera, g.GameData, g.ResourceManager, g);
			world = new GameWorld(sysrender);
			world.LoadSystem(sys, g.ResourceManager);
			g.Sound.PlayMusic(sys.MusicSpace);
			camera.UpdateProjection ();

			trender = new Renderer2D (Game.RenderState);
			font = g.Fonts.GetSystemFont("Agency FB");
			g.Keyboard.KeyDown += G_Keyboard_KeyDown;
			g.Keyboard.TextInput += G_Keyboard_TextInput;
		}
		const double MSG_TIMER = 3;
		double msg_current_time = 0;
		string current_msg = "";

		public override void Update (TimeSpan delta)
		{
			if (current_msg != "")
			{
				msg_current_time -= delta.TotalSeconds;
				if (msg_current_time < 0)
					current_msg = "";
			}
			if (!textEntry)
				ProcessInput(delta);
			else
			{
				if (Game.Keyboard.IsKeyDown(Keys.Enter))
				{
					textEntry = false;
					Game.DisableTextInput();
					if (Game.GameData.SystemExists(currentText.Trim()))
					{
						sys = Game.GameData.GetSystem(currentText.Trim());
						world.LoadSystem(sys, Game.ResourceManager);
						camera.Free = false;
						camera.Update(TimeSpan.FromSeconds(1));
						camera.Free = true;
						Game.Sound.PlayMusic(sys.MusicSpace);
					}
					else
					{
						msg_current_time = MSG_TIMER;
						current_msg = string.Format("{0} is not a valid system", currentText.Trim());
					}
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
			if (e.Key == Keys.L && !textEntry)
			{
				Game.Screenshots.TakeScreenshot();
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
				camera.MoveVector = Vector3.Forward;
			}
			if (Game.Keyboard.IsKeyDown(Keys.S))
			{
				camera.MoveVector = Vector3.Backward;
			}
			if (Game.Keyboard.IsKeyDown(Keys.A))
			{
				camera.MoveVector = Vector3.Left;
			}
			if (Game.Keyboard.IsKeyDown(Keys.D))
			{
				camera.MoveVector = Vector3.Right;
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
			//DebugDrawing.DrawShadowedText (trender, font, 16, string.Format(DEMO_TEXT,camera.Position.X, camera.Position.Y, camera.Position.Z, sys.Id, sys.Name, DebugDrawing.SizeSuffix(GC.GetTotalMemory(false)), Game.Renderer), 5, 5);

			if (textEntry)
			{
				DebugDrawing.DrawShadowedText(trender, font, 16, "Change System (Esc to cancel): " + currentText, 5, 200);
			}
			if (current_msg != null)
			{
				DebugDrawing.DrawShadowedText(trender, font, 16, current_msg, 5, 230, Color4.Red);
			}
			trender.Finish ();
		}
	}
}

