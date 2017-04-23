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
Selected Object (right click): {7}
Mouse Position: {8} {9}
Mouse Flight: {10}
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
		Color4[] colors = new Color4[]
		{
			Color4.White,
			Color4.Red,
			Color4.Green,
			Color4.Blue,
			Color4.Yellow,
			Color4.Pink
		};
		Cursor cur;
		Hud hud;
		EngineComponent ecpt;
		public DemoGameplay(FreelancerGame g) : base(g)
		{
			FLLog.Info("Game", "Starting Gameplay Demo");
			sys = g.GameData.GetSystem("li01");
			var shp = g.GameData.GetShip("li_elite");
			//Set up player object + camera
			player = new GameObject(shp.Drawable, g.ResourceManager, false);
			player.PhysicsComponent.Position = new JVector(-31000, 0, -26755);
			player.PhysicsComponent.Material.Restitution = 1;
			camera = new ChaseCamera(Game.Viewport);
			camera.ChasePosition = new Vector3(-31000, 0, -26755);
			camera.ChaseOrientation = player.PhysicsComponent.Orientation.ToOpenTK();
			camera.Reset();

			sysrender = new SystemRenderer(camera, g.GameData, g.ResourceManager);
			world = new GameWorld(sysrender);
			world.LoadSystem(sys, g.ResourceManager);
			world.Objects.Add(player);
			world.Physics.SetDampingFactors(0.01f, 1f);
			world.RenderUpdate += World_RenderUpdate;
			world.PhysicsUpdate += World_PhysicsUpdate;
			var eng = new GameData.Items.Engine() { FireEffect = "gf_li_smallengine02_fire" };
			player.Components.Add((ecpt = new EngineComponent(player, eng, g)));
			ecpt.Speed = 0;
			player.Register(sysrender, world.Physics);
			g.Sound.PlayMusic(sys.MusicSpace);
			trender = new Renderer2D(Game.RenderState);
			font = Font.FromSystemFont(trender, "Agency FB", 16);
			g.Keyboard.KeyDown += G_Keyboard_KeyDown;
			g.Keyboard.TextInput += G_Keyboard_TextInput;
			debugphysics = new PhysicsDebugRenderer();
			cur = g.ResourceManager.GetCursor("arrow");
			hud = new Hud(g);
			g.Keyboard.KeyDown += (e) =>
			{
				if (e.Key == Keys.L)
				{
					g.Screenshots.TakeScreenshot();
				}
			};
		}

		void World_RenderUpdate(TimeSpan delta)
		{
			//Has to be here or glitches
			camera.ChasePosition = player.PhysicsComponent.Position.ToOpenTK();
			camera.ChaseOrientation = player.PhysicsComponent.Orientation.ToOpenTK();
			camera.Update(delta);
		}

		public override void Update(TimeSpan delta)
		{
			ecpt.Speed = (Velocity / MAX_VELOCITY) * 0.9f;
			hud.Velocity = Velocity;
			world.Update(delta);
		}

		void World_PhysicsUpdate(TimeSpan delta)
		{
			player.PhysicsComponent.LinearVelocity *= 0.8f;
			if (player.PhysicsComponent.LinearVelocity.Length() < Velocity)
			{
				player.PhysicsComponent.ApplyImpulse(JVector.Transform(JVector.Forward, player.PhysicsComponent.Orientation) * player.PhysicsComponent.Mass * 40);
			}
			ProcessInput(delta);
		}
		bool mouseFlight = false;
		void G_Keyboard_KeyDown(KeyEventArgs e)
		{
			if (e.Key == Keys.Backspace && textEntry)
			{
				if (currentText.Length > 0)
				{
					currentText = currentText.Substring(0, currentText.Length - 1);
				}
			}
			if (e.Key == Keys.Space && !textEntry)
			{
				mouseFlight = !mouseFlight;
			}
			if (e.Key == Keys.P && !textEntry)
			{
				Game.RenderState.Wireframe = !Game.RenderState.Wireframe;
			}
		}

		void G_Keyboard_TextInput(string text)
		{
			if (textEntry)
				currentText += text;

		}
		Vector2 moffset = Vector2.Zero;
		const float ACCEL = 85;
		GameObject selected;
		void ProcessInput(TimeSpan delta)
		{
			moffset = (new Vector2(Game.Mouse.X, Game.Mouse.Y) - new Vector2(Game.Width / 2, Game.Height / 2));
			moffset *= new Vector2(1f / Game.Width, 1f / Game.Height);

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

			var pc = player.PhysicsComponent;
			if (Game.Mouse.IsButtonDown(MouseButtons.Left) || mouseFlight)
			{
				float rotateSpeed = 0.03f;
				pc.Orientation = JMatrix.CreateFromYawPitchRoll(-moffset.X * rotateSpeed, -moffset.Y * rotateSpeed, 0) * pc.Orientation;
			}
			else
			{
				double pitch, yaw, roll;
				DecomposeOrientation(pc.Orientation, out pitch, out yaw, out roll);
				var lerped = MathHelper.Lerp((float)roll, 0, 0.007f);
				pc.Orientation = JMatrix.CreateFromYawPitchRoll((float)yaw, (float)pitch, lerped);
			}

			if (Game.Mouse.IsButtonDown(MouseButtons.Right))
			{
				var newselected = GetSelection(Game.Mouse.X, Game.Mouse.Y);
				if (selected != null && newselected != selected)
					selected.PhysicsComponent.EnableDebugDraw = false;
				if (newselected != null)
				{
					newselected.PhysicsComponent.EnableDebugDraw = true;
					debugDrawBody = newselected.PhysicsComponent;
				}
				else
				{
					debugDrawBody = null;
				}
				selected = newselected;
			}

			if (Game.Keyboard.IsKeyDown(Keys.Up))
			{
				camera.DesiredPositionOffset -= (10 * (float)delta.TotalSeconds * camera.OffsetDirection);
			}
			else if (Game.Keyboard.IsKeyDown(Keys.Down))
			{
				camera.DesiredPositionOffset += (10 * (float)delta.TotalSeconds * camera.OffsetDirection);
			}

			if (Game.Keyboard.IsKeyDown(Keys.Left))
			{
				camera.DesiredPositionOffset.X += 10 * (float)delta.TotalSeconds;
			}
			if (Game.Keyboard.IsKeyDown(Keys.Right))
			{
				camera.DesiredPositionOffset.X -= 10 * (float)delta.TotalSeconds;
			}
		}

		GameObject GetSelection(float x, float y)
		{
			var vp = new Vector2(Game.Width, Game.Height);

			var start = UnProject(new Vector3(x, y, 0f), camera.Projection, camera.View, vp).ToJitter();
			var end = UnProject(new Vector3(x, y, 1f), camera.Projection, camera.View, vp).ToJitter();

			var dir = end;
			dir.Normalize();
			dir *= 200000;
			RigidBody rb;
			JVector normal;
			float fraction;
			var result = world.Physics.CollisionSystem.Raycast(
				start,
				dir,
				RaycastCallback,
				out rb,
				out normal,
				out fraction
			);
			if (result && rb.Tag is GameObject)
				return (GameObject)rb.Tag;
			return null;
		}

		bool RaycastCallback(RigidBody body, JVector normal, float fraction)
		{
			return body.Tag != player;
		}

		static Vector3 UnProject(Vector3 mouse, Matrix4 projection, Matrix4 view, Vector2 viewport)
		{
			Vector4 vec;

			vec.X = 2.0f * mouse.X / (float)viewport.X - 1;
			vec.Y = -(2.0f * mouse.Y / (float)viewport.Y - 1);
			vec.Z = mouse.Z;
			vec.W = 1.0f;

			Matrix4 viewInv = Matrix4.Invert(view);
			Matrix4 projInv = Matrix4.Invert(projection);

			Vector4.Transform(ref vec, ref projInv, out vec);
			Vector4.Transform(ref vec, ref viewInv, out vec);

			if (vec.W > 0.000001f || vec.W < -0.000001f)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}

			return vec.Xyz;
		}

		static void DecomposeOrientation(JMatrix mx, out double xPitch, out double yYaw, out double zRoll)
		{
			xPitch = Math.Asin(-mx.M32);
			double threshold = 0.001; // Hardcoded constant – burn him, he’s a witch
			double test = Math.Cos(xPitch);

			if (test > threshold)
			{
				zRoll = Math.Atan2(mx.M12, mx.M22);
				yYaw = Math.Atan2(mx.M31, mx.M33);
			}
			else
			{
				zRoll = Math.Atan2(-mx.M21, mx.M11);
				yYaw = 0.0;
			}
		}

		RigidBody debugDrawBody;
		public override void Draw(TimeSpan delta)
		{
			sysrender.Draw();
			int i = 0;
			int j = 0;
			debugphysics.StartFrame(camera, Game.RenderState);
			/*foreach (RigidBody body in world.Physics.RigidBodies)
			{
				debugphysics.Color = colors[i];
				if (j == draw_hitboxes)
				{
					if (!body.EnableDebugDraw)
						body.EnableDebugDraw = true;
					body.DebugDraw(debugphysics);
				}
				i++;
				j++;
				if (i >= colors.Length)
					i = 0;
			}*/
			if (debugDrawBody != null)
			{
				debugDrawBody.DebugDraw(debugphysics);
			}

			debugphysics.Render();
			hud.Draw(Game);
			trender.Start(Game.Width, Game.Height);
			string sel_obj = "None";
			if (selected != null)
			{
				if (selected.Name == null)
					sel_obj = "unknown object";
				else
					sel_obj = selected.Name;
			}
			DrawShadowedText(string.Format(DEMO_TEXT, camera.Position.X, camera.Position.Y, camera.Position.Z, sys.Id, sys.Name, SizeSuffix(GC.GetTotalMemory(false)), Velocity, sel_obj, moffset.X, moffset.Y, mouseFlight), 5, 5);
			cur.Draw(trender, Game.Mouse);
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
