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
using Jitter;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
namespace LibreLancer
{
	public class DemoGameplay : GameState
	{
		const string DEMO_TEXT =
@"GAMEPLAY DEMO
{3} ({4})
Camera Position: (X: {0:0.00}, Y: {1:0.00}, Z: {2:0.00})
C# Memory Usage: {5}
Velocity: {6}
Hitbox Drawing (H/J): {7}
";
		private const float ROTATION_SPEED = 1f;
		GameData.StarSystem sys;
		GameWorld world;
		ChaseCamera camera;
		PhysicsDebugRenderer debugphysics;
		SystemRenderer sysrender;
		bool wireframe = false;
		Renderer2D trender;
		Font font;
		bool textEntry = false;
		string currentText = "";
		GameObject player;
		public float Velocity = 0f;
		const float MAX_VELOCITY = 320f;
		int draw_hitboxes = 0;
		Color4[] colors = new Color4[]
		{
			Color4.White,
			Color4.Red,
			Color4.Green,
			Color4.Blue,
			Color4.Yellow,
			Color4.Pink
		};
		public DemoGameplay(FreelancerGame g) : base(g)
		{
			FLLog.Info("Game", "Starting Gameplay Demo");
			sys = g.GameData.GetSystem("li01");
			var shp = g.GameData.GetShip("li_elite");
			player = new GameObject(shp.Drawable, g.ResourceManager, false);
			//player.PhysicsComponent = new RigidBody(new CapsuleShape(100, 25));
			player.PhysicsComponent.Position = new JVector(-31000, 0, -26755);
			camera = new ChaseCamera(Game.Viewport);
			//camera.Up = VectorMath.UnitY;
			camera.ChasePosition = new Vector3(-31000, 0, -26755);
			//camera.ChaseDirection = VectorMath.Forward;
			//camera.Reset();
			sysrender = new SystemRenderer(camera, g.GameData, g.ResourceManager);
			world = new GameWorld(sysrender);
			world.LoadSystem(sys, g.ResourceManager);
			world.Objects.Add(player);
			world.Physics.SetDampingFactors(0.5f, 1f);
			world.RenderUpdate += World_RenderUpdate;
			player.Components.Add(new EngineComponent(player, null));
			player.Register(sysrender, world.Physics);
			g.Sound.PlayMusic(sys.MusicSpace);
			trender = new Renderer2D(Game.RenderState);
			font = Font.FromSystemFont(trender, "Agency FB", 16);
			g.Keyboard.KeyDown += G_Keyboard_KeyDown;
			g.Keyboard.TextInput += G_Keyboard_TextInput;
			debugphysics = new PhysicsDebugRenderer();
			foreach (RigidBody body in world.Physics.RigidBodies)
				body.EnableDebugDraw = true;
		}

		void World_RenderUpdate(TimeSpan delta)
		{
			//Has to be here or glitches
			camera.ChasePosition = player.PhysicsComponent.Position.ToOpenTK();
			camera.Update(delta);
		}

		public override void Update(TimeSpan delta)
		{
			if (player.PhysicsComponent.LinearVelocity.Length() < Velocity)
			{
				player.PhysicsComponent.ApplyImpulse(JVector.Transform(JVector.Forward, player.PhysicsComponent.Orientation) * 40);
			}
			ProcessInput(delta);
			world.Update(delta);
		}

		void G_Keyboard_KeyDown(KeyEventArgs e)
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
			if (e.Key == Keys.H)
			{
				draw_hitboxes++;
				if (draw_hitboxes >= world.Physics.RigidBodies.Count)
					draw_hitboxes = 0;
			}
			if (e.Key == Keys.J)
			{
				draw_hitboxes--;
				if (draw_hitboxes <= 0)
					draw_hitboxes = world.Physics.RigidBodies.Count - 1;
			}
		}

		void G_Keyboard_TextInput(string text)
		{
			if (textEntry)
				currentText += text;

		}
		const float ACCEL = 85;
		void ProcessInput(TimeSpan delta)
		{
			if (Game.Keyboard.IsKeyDown(Keys.W))
			{
				Velocity += (float)(delta.TotalSeconds * ACCEL);
				Velocity = MathHelper.Clamp(Velocity, 0, MAX_VELOCITY);
			}
			else if (Game.Keyboard.IsKeyDown(Keys.S))
			{
				Velocity -= (float)(delta.TotalSeconds * ACCEL);
				Velocity = MathHelper.Clamp(Velocity, 0, MAX_VELOCITY);
			}
		}

		public override void Draw(TimeSpan delta)
		{
			sysrender.Draw();
			int i = 0;
			int j = 0;
			debugphysics.StartFrame(camera, Game.RenderState);
			foreach (RigidBody body in world.Physics.RigidBodies)
			{
				debugphysics.Color = colors[i];
				if (j == draw_hitboxes)
				{
					body.DebugDraw(debugphysics);
				}
				i++;
				j++;
				if (i >= colors.Length)
					i = 0;
			}
			debugphysics.Render();
			trender.Start(Game.Width, Game.Height);
			DrawShadowedText(string.Format(DEMO_TEXT, camera.Position.X, camera.Position.Y, camera.Position.Z, sys.Id, sys.Name, SizeSuffix(GC.GetTotalMemory(false)), Velocity, draw_hitboxes), 5, 5);
			trender.Finish();
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
			trender.DrawString(font,
				text,
				x + 2, y + 2,
				Color4.Black);
			trender.DrawString(font,
				text,
				x, y,
				Color4.White);
		}
	}
}
