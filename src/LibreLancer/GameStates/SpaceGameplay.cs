// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Physics;
namespace LibreLancer
{
	public class SpaceGameplay : GameState
	{

		const string DEMO_TEXT =
@"GAMEPLAY DEMO
{3} ({4})
Camera Position: (X: {0:0.00}, Y: {1:0.00}, Z: {2:0.00})
C# Memory Usage: {5}
Velocity: {6}
Selected Object: {7}
Mouse Position: {8} {9}
Mouse Flight: {10}
";
		private const float ROTATION_SPEED = 1f;
		GameData.StarSystem sys;
		public GameWorld world;
        public FreelancerGame FlGame => Game;
		ChaseCamera camera;
		PhysicsDebugRenderer debugphysics;
		SystemRenderer sysrender;
		bool wireframe = false;
		Font font;
		bool textEntry = false;
		string currentText = "";
		public GameObject player;
		ShipControlComponent control;
        WeaponControlComponent weapons;
		PowerCoreComponent powerCore;
		public float Velocity = 0f;
		const float MAX_VELOCITY = 80f;
		Cursor cur_arrow;
		Cursor cur_reticle;
		Cursor current_cur;
        ScriptedHud hud;
		EngineComponent ecpt;
		InputManager input;
		GameSession session;
        bool loading = true;
        LoadingScreen loader;
        public Cutscene Thn;
		public SpaceGameplay(FreelancerGame g, GameSession session) : base(g)
		{
			FLLog.Info("Game", "Entering system " + session.PlayerSystem);
            g.ResourceManager.ClearTextures(); //Do before loading things
            this.session = session;
            font = Game.Fonts.GetSystemFont("Agency FB");

            sys = new GameData.StarSystem();
            loader = new LoadingScreen(g, g.GameData.FillSystem(session.PlayerSystem, sys));
		}

        void FinishLoad()
        {
            var shp = Game.GameData.GetShip(session.PlayerShip);
            //Set up player object + camera
            player = new GameObject(shp.Drawable, Game.ResourceManager, false);
            control = new ShipControlComponent(player);
            control.Ship = shp;
            player.Components.Add(control);
            weapons = new WeaponControlComponent(player);
            player.Components.Add(weapons);
            powerCore = new PowerCoreComponent(player)
            {
                ThrustCapacity = 1000,
                ThrustChargeRate = 100
            };
            player.Components.Add(powerCore);
            player.Transform = new Matrix4(session.PlayerOrientation) * Matrix4.CreateTranslation(session.PlayerPosition);
            player.PhysicsComponent.Mass = shp.Mass;
            if(shp.Mass < 0)
            {
                FLLog.Error("Ship", "Mass < 0");
            }
            player.Nickname = "player";
            foreach (var equipment in session.MountedEquipment)
            {
                var equip = Game.GameData.GetEquipment(equipment.Value);
                if (equip == null) continue;
                var obj = new GameObject(equip, player.GetHardpoint(equipment.Key), player);
                player.Children.Add(obj);
            }

            camera = new ChaseCamera(Game.Viewport);
            camera.ChasePosition = session.PlayerPosition;
            camera.ChaseOrientation = player.Transform.ClearTranslation();
            var offset = shp.ChaseOffset;

            camera.DesiredPositionOffset = offset;
            camera.Reset();

            sysrender = new SystemRenderer(camera, Game.GameData, Game.ResourceManager, Game);
            world = new GameWorld(sysrender);
            world.LoadSystem(sys, Game.ResourceManager);

            world.Objects.Add(player);
            world.RenderUpdate += World_RenderUpdate;
            world.PhysicsUpdate += World_PhysicsUpdate;
            var eng = new GameData.Items.Engine() { FireEffect = "gf_li_smallengine02_fire", LinearDrag = 600, MaxForce = 48000 };
            player.Components.Add((ecpt = new EngineComponent(player, eng, Game)));
            ecpt.Speed = 0;
            player.Register(world.Physics);
            Game.Sound.PlayMusic(sys.MusicSpace);
            Game.Keyboard.TextInput += G_Keyboard_TextInput;
            debugphysics = new PhysicsDebugRenderer();
            //world.Physics.EnableWireframes(debugphysics);
            cur_arrow = Game.ResourceManager.GetCursor("cross");
            cur_reticle = Game.ResourceManager.GetCursor("fire_neutral");
            current_cur = cur_arrow;
            Game.Keyboard.TextInput += Game_TextInput;
            Game.Keyboard.KeyDown += Keyboard_KeyDown;
            Game.Mouse.MouseDown += Mouse_MouseDown;
            input = new InputManager(Game);
            input.ToggleActivated += Input_ToggleActivated;
            input.ToggleUp += Input_ToggleUp;
            pilotcomponent = new AutopilotComponent(player);
            pilotcomponent.DockComplete += Pilotcomponent_DockComplete;
            player.Components.Add(pilotcomponent);
            player.World = world;
            world.MessageBroadcasted += World_MessageBroadcasted;
            world.Physics.EnableWireframes(sysrender.DebugRenderer);
            ConstructHud();
            FadeIn(0.5, 0.5);
        }
        class LuaAPI
        {
            SpaceGameplay g;
            public LuaAPI(SpaceGameplay gameplay)
            {
                this.g = gameplay;   
            }
            public Neo.IronLua.LuaTable maneuvers()
            {
                var list = new Neo.IronLua.LuaTable();
                foreach(var maneuver in g.Game.GameData.GetManeuvers()) {
                    var mn = (dynamic)(new Neo.IronLua.LuaTable());
                    mn.action = maneuver.Action;
                    mn.activemodel = "//" + maneuver.ActiveModel;
                    mn.inactivemodel = "//" + maneuver.InactiveModel;
                    mn.infocarda = maneuver.InfocardA;
                    mn.infocardb = maneuver.InfocardB;
                    g.hud.UI.TableInsert(list, mn);
                }
                return list;
            }
            public bool setmaneuver(string e) => g.ManeuverSelect(e);
            public int thrustpct() => ((int)(g.powerCore.CurrentThrustCapacity / g.powerCore.ThrustCapacity * 100));
            public int speed() => ((int)g.player.PhysicsComponent.Body.LinearVelocity.Length);
            public bool multiplayer() => false;
        }
		void World_MessageBroadcasted(GameObject sender, GameMessageKind kind)
		{
			switch (kind)
			{
				case GameMessageKind.ManeuverFinished:
					hud.SetManeuver("FreeFlight");
					break;
			}
		}

		void Pilotcomponent_GotoComplete()
		{
			Console.WriteLine("Went to object!");
		}

		void Pilotcomponent_DockComplete(DockAction action)
		{
			pilotcomponent.CurrentBehaviour = AutopilotBehaviours.None;
			if (action.Kind == DockKinds.Base)
			{
				Game.ChangeState(new RoomGameplay(Game, session, action.Target));
			}
			else if(action.Kind == DockKinds.Jump)
			{
				session.JumpTo(action.Target, action.Exit);
			}
		}

		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
			input.Dispose();
			hud.Dispose();
			sysrender.Dispose();
            world.Dispose();
		}

		void Keyboard_KeyDown(KeyEventArgs e)
		{
			if (hud.TextEntry)
			{
				hud.TextEntryKeyPress(e.Key);
				if (hud.TextEntry == false) Game.DisableTextInput();
			}
			else
			{
				if (e.Key == Keys.L)
				{
					Game.Screenshots.TakeScreenshot();
				}
				if (e.Key == Keys.Enter)
				{
					hud.TextEntry = true;
					Game.EnableTextInput();
				}
			}
		}

		void Game_TextInput(string text)
		{
			hud.OnTextEntry(text);
		}

		bool dogoto = false;
		AutopilotComponent pilotcomponent = null;
		void Hud_OnTextEntry(string obj)
		{
			var sp = obj.Split('>');
			switch (sp[0])
			{
				case "animate":
					if (selected == null) return;
					var component = selected.GetComponent<AnimationComponent>();
					if (component != null)
						component.StartAnimation(sp[1].Trim(), false);
					break;
				case "wireframe":
					//selected.PhysicsComponent.EnableDebugDraw = true;
					//debugDrawBody = selected.PhysicsComponent;
					break;
                case "reloadxml":
                    newHud = true;
                    break;
			}
			session.ProcessConsoleCommand(obj);
		}

		bool ManeuverSelect(string e)
		{
			switch (e)
			{
				case "FreeFlight":
					pilotcomponent.CurrentBehaviour = AutopilotBehaviours.None;
					return true;
				case "Dock":
					if (selected == null) return false;
					DockComponent d;
					if ((d = selected.GetComponent<DockComponent>()) != null)
					{
						pilotcomponent.TargetObject = selected;
						pilotcomponent.CurrentBehaviour = AutopilotBehaviours.Dock;
						return true;
					}
					return false;
				case "Goto":
					if (selected == null) return false;
					pilotcomponent.TargetObject = selected;
					pilotcomponent.CurrentBehaviour = AutopilotBehaviours.Goto;
					return true;
			}
			return false;
		}

		void World_RenderUpdate(TimeSpan delta)
		{
            //Has to be here or glitches
            if (!Game.Keyboard.IsKeyDown(Keys.U))
            {
                camera.ChasePosition = player.PhysicsComponent.Body.Position;
                camera.ChaseOrientation = player.PhysicsComponent.Body.Transform.ClearTranslation();
            }
			camera.Update(delta);
		}

        bool newHud = false;
        void ConstructHud()
        {
            hud = new ScriptedHud(new LuaAPI(this), true, Game);
            hud.OnEntered += Hud_OnTextEntry;
            hud.Init();
            hud.SetManeuver("FreeFlight");
        }

        public override void OnResize()
        {
            camera.Viewport = Game.Viewport;
        }

        public bool ShowHud = true;

        public override void Update(TimeSpan delta)
		{
            if(loading)
            {
                if(loader.Update(delta))
                {
                    loading = false;
                    loader = null;
                    FinishLoad();
                }
                return;
            }
            if (session.Update(this, delta)) return;
            if (newHud) {
                hud.Dispose();
                ConstructHud();
                newHud = false;
            }
            if(ShowHud && (Thn == null || !Thn.Running))
                hud.Update(delta);
            if (Thn != null && Thn.Running)
            {
                Thn.Update(delta);
                sysrender.Camera = Thn.CameraHandle;
            }
            else
                sysrender.Camera = camera;
			world.Update(delta);
		}

		bool cruise = false;
		bool thrust = false;

		void World_PhysicsUpdate(TimeSpan delta)
		{
			control.EngineState = cruise ? EngineStates.Cruise : EngineStates.Standard;
            if(Thn == null || !Thn.Running)
			    ProcessInput(delta);
		}

		bool mouseFlight = false;

		void Input_ToggleActivated(int id)
		{
			if (hud.TextEntry) return;
			switch (id)
			{
				case InputAction.ID_TOGGLECRUISE:
					cruise = !cruise;
					break;
				case InputAction.ID_TOGGLEMOUSEFLIGHT:
					mouseFlight = !mouseFlight;
					break;
				case InputAction.ID_THRUST:
					control.ThrustEnabled = true;
					break;
			}
		}

		void Input_ToggleUp(int obj)
		{
			if (obj == InputAction.ID_THRUST)
				control.ThrustEnabled = false;
		}

		void G_Keyboard_TextInput(string text)
		{
			if (textEntry)
				currentText += text;

        }

        double lastDown = -1000;

        void Mouse_MouseDown(MouseEventArgs e)
        {
            if((e.Buttons & MouseButtons.Left) > 0)
            {
                lastDown = Game.TotalTime;
            }
        }


        Vector2 moffset = Vector2.Zero;
		const float ACCEL = 85;
		GameObject selected;
		void ProcessInput(TimeSpan delta)
		{
			moffset = (new Vector2(Game.Mouse.X, Game.Mouse.Y) - new Vector2(Game.Width / 2, Game.Height / 2));
			moffset *= new Vector2(1f / Game.Width, 1f / Game.Height);
			moffset *= 2;

			input.Update();

			if (!hud.TextEntry)
			{
				if (input.ActionDown(InputAction.ID_THROTTLEUP))
				{
                    control.EnginePower += (float)(delta.TotalSeconds);
					control.EnginePower = MathHelper.Clamp(control.EnginePower, 0, 1);
				}

				else if (input.ActionDown(InputAction.ID_THROTTLEDOWN))
				{
                    control.EnginePower -= (float)(delta.TotalSeconds);
                    control.EnginePower = MathHelper.Clamp(control.EnginePower, 0, 1);
				}
			}

			StrafeControls strafe = StrafeControls.None;
			if (!hud.TextEntry)
			{
				if (input.ActionDown(InputAction.ID_STRAFELEFT)) strafe |= StrafeControls.Left;
				if (input.ActionDown(InputAction.ID_STRAFERIGHT)) strafe |= StrafeControls.Right;
				if (input.ActionDown(InputAction.ID_STRAFEUP)) strafe |= StrafeControls.Up;
				if (input.ActionDown(InputAction.ID_STRAFEDOWN)) strafe |= StrafeControls.Down;
			}

			var pc = player.PhysicsComponent;
			if (Game.Mouse.IsButtonDown(MouseButtons.Left) || mouseFlight)
			{
				control.PlayerPitch = -moffset.Y;
				control.PlayerYaw = -moffset.X;

                var mX = Game.Mouse.X;
                var mY = Game.Mouse.Y;
                camera.MousePosition = new Vector2(
                    mX, Game.Height - mY
                );
                camera.MouseFlight = true;
            }
			else
			{
				control.PlayerPitch = control.PlayerYaw = 0;
                camera.MouseFlight = false;
			}

			control.CurrentStrafe = strafe;
			//control.EnginePower = Velocity / MAX_VELOCITY;
			var obj = GetSelection(Game.Mouse.X, Game.Mouse.Y);
			current_cur = obj == null ? cur_arrow : cur_reticle;
			
            var ep = UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 0.25f), camera.Projection, camera.View, new Vector2(Game.Width, Game.Height));
            var tgt = UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 0f), camera.Projection, camera.View, new Vector2(Game.Width, Game.Height));
            var dir = (tgt - ep).Normalized();
            var dir2 = new Matrix3(player.PhysicsComponent.Body.Transform.ClearTranslation()) * Vector3.UnitZ;
            tgt += dir * 750;
            weapons.AimPoint = tgt;

            if(!Game.Mouse.IsButtonDown(MouseButtons.Left) && Game.TotalTime - lastDown < 0.25)
            {
                var newselected = GetSelection(Game.Mouse.X, Game.Mouse.Y);
                if (newselected != null) selected = newselected;
            }
            if (Game.Mouse.IsButtonDown(MouseButtons.Right))
                weapons.FireAll();
        }

		GameObject GetSelection(float x, float y)
		{
			var vp = new Vector2(Game.Width, Game.Height);

			var start = UnProject(new Vector3(x, y, 0f), camera.Projection, camera.View, vp);
			var end = UnProject(new Vector3(x, y, 1f), camera.Projection, camera.View, vp);
			var dir = end;

			PhysicsObject rb;
			var result = SelectionCast(
				start,
				dir,
				800000,
				out rb
			);
			if (result && rb.Tag is GameObject)
				return (GameObject)rb.Tag;
			return null;
		}

		//Select by bounding box, not by mesh
		bool SelectionCast(Vector3 rayOrigin, Vector3 direction, float maxDist, out PhysicsObject body)
		{
			float dist = float.MaxValue;
			body = null;
			var jitterDir = direction * maxDist;
			foreach (var rb in world.Physics.Objects)
			{
				if (rb.Tag == player) continue;
				if (rb.Collider is SphereCollider)
				{
					//Test spheres
					var sph = (SphereCollider)rb.Collider;
					if (SphereRayIntersect(rayOrigin, direction, maxDist, rb.Position, sph.Radius))
					{
						var nd = VectorMath.DistanceSquared(rb.Position, camera.Position);
						if (nd < dist)
						{
							dist = nd;
							body = rb;
						}
					}
				}
				else
				{
					//var tag = rb.Tag as GameObject;
                    if (!rb.GetBoundingBox().RayIntersect(ref rayOrigin, ref jitterDir)) continue;
                    body = rb;
					/*if (tag == null || tag.CmpParts.Count == 0)
					{
						//Single part
						var nd = VectorMath.DistanceSquared(rb.Position, camera.Position);
						if (nd < dist)
						{
							dist = nd;
							body = rb;
						}
					}
					else
					{
						//Test by cmp parts
						var sh = (CompoundSurShape)rb.Shape;
						for (int i = 0; i < sh.Shapes.Length; i++)
						{
							sh.Shapes[i].UpdateBoundingBox();
							var bb = sh.Shapes[i].BoundingBox;
							bb.Min += rb.Position;
							bb.Max += rb.Position;
							if (bb.RayIntersect(ref rayOrigin, ref jitterDir))
							{
								
								var nd = VectorMath.DistanceSquared(rb.Position, camera.Position);
								if (nd < dist)
								{
									dist = nd;
									body = rb;
								}
								break;
							}
						}
					}*/
				}
			}
			return body != null;
		}

		static bool SphereRayIntersect(Vector3 rayOrigin, Vector3 d, float maxdistance, Vector3 centre, float radius)
		{
			var dist = VectorMath.DistanceSquared(rayOrigin, centre);
			if (dist > (maxdistance - radius) * (maxdistance - radius)) return false;
			//Ray start offset from sphere centre
			var p = rayOrigin - centre;
			float rSquared = radius * radius;
			float p_d = Vector3.Dot(p, d);
			if (p_d > 0 || Vector3.Dot(p, p) < rSquared)
				return false;
			var a = p - p_d * d;
			var aSquared = Vector3.Dot(a, a);
			return aSquared < rSquared;
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

		//RigidBody debugDrawBody;
		public override void Draw(TimeSpan delta)
		{
            RenderMaterial.VertexLighting = false;
            if (loading)
            {
                loader.Draw(delta);
                return;
            }
            sysrender.Draw();

            sysrender.DebugRenderer.StartFrame(camera, Game.RenderState);
            sysrender.DebugRenderer.Render();
            if((Thn == null || !Thn.Running) && ShowHud)
                hud.Draw(delta);
			Game.Renderer2D.Start(Game.Width, Game.Height);
            if ((Thn == null || !Thn.Running) && ShowHud)
            {
                string sel_obj = "None";
                if (selected != null)
                {
                    if (selected.Name == null)
                        sel_obj = "unknown object";
                    else
                        sel_obj = selected.Name;
                }
                DebugDrawing.DrawShadowedText(Game.Renderer2D, font, 16, string.Format(DEMO_TEXT, camera.Position.X, camera.Position.Y, camera.Position.Z, sys.Id, sys.Name, DebugDrawing.SizeSuffix(GC.GetTotalMemory(false)), Velocity, sel_obj, moffset.X, moffset.Y, mouseFlight), 5, 5);
                current_cur.Draw(Game.Renderer2D, Game.Mouse);
            }
            DoFade(delta);
			Game.Renderer2D.Finish();
		}
	}
}
