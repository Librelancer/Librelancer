// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Interface;
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
Pitch: {8:0.00}
Yaw: {9:0.00}
Roll: {10:0.00}
Mouse Flight: {11}
";
		private const float ROTATION_SPEED = 1f;
		GameData.StarSystem sys;
		public GameWorld world;
        public FreelancerGame FlGame => Game;
		ChaseCamera camera;
		SystemRenderer sysrender;
		bool wireframe = false;
		bool textEntry = false;
		string currentText = "";
		public GameObject player;
		ShipPhysicsComponent control;
        ShipInputComponent shipInput;
        WeaponControlComponent weapons;
		PowerCoreComponent powerCore;
		public float Velocity = 0f;
		const float MAX_VELOCITY = 80f;
		Cursor cur_arrow;
		Cursor cur_reticle;
		Cursor current_cur;
		CEngineComponent ecpt;
		InputManager input;
		CGameSession session;
        bool loading = true;
        LoadingScreen loader;
        public Cutscene Thn;
        DebugGraph pyw;
        private UiContext ui;
        private UiWidget widget;
        private LuaAPI uiApi;
		public SpaceGameplay(FreelancerGame g, CGameSession session) : base(g)
		{
			FLLog.Info("Game", "Entering system " + session.PlayerSystem);
            g.ResourceManager.ClearTextures(); //Do before loading things
            this.session = session;
            sys = g.GameData.GetSystem(session.PlayerSystem);
            ui = Game.Ui;
            ui.GameApi = uiApi = new LuaAPI(this);
            loader = new LoadingScreen(g, g.GameData.LoadSystemResources(sys));
            loader.Init();
        }

        void FinishLoad()
        {
            ui.OpenScene("hud");
            var shp = Game.GameData.GetShip(session.PlayerShip);
            //Set up player object + camera
            player = new GameObject(shp.ModelFile.LoadFile(Game.ResourceManager), Game.ResourceManager);
            control = new ShipPhysicsComponent(player);
            control.Ship = shp;
            shipInput = new ShipInputComponent(player);
            player.Components.Add(shipInput);
            player.Components.Add(control);
            weapons = new WeaponControlComponent(player);
            player.Components.Add(weapons);
            player.SetLocalTransform(session.PlayerOrientation * Matrix4x4.CreateTranslation(session.PlayerPosition));
            player.PhysicsComponent.Mass = shp.Mass;
            if(shp.Mass < 0)
            {
                FLLog.Error("Ship", "Mass < 0");
            }
            player.Nickname = "player";
            foreach (var equipment in session.Mounts)
            {
                var equip = Game.GameData.GetEquipment(equipment.Item);
                if (equip == null) continue;
                EquipmentObjectManager.InstantiateEquipment(player, Game.ResourceManager, true, equipment.Hardpoint, equip);
            }
            powerCore = player.GetComponent<PowerCoreComponent>();
            if (powerCore == null) throw new Exception("Player launched without a powercore equipped!");
            camera = new ChaseCamera(Game.Viewport, Game.GameData.Ini.Cameras);
            camera.ChasePosition = session.PlayerPosition;
            camera.ChaseOrientation = player.LocalTransform.ClearTranslation();
            var offset = shp.ChaseOffset;
            
            camera.DesiredPositionOffset = offset;
            if (shp.CameraHorizontalTurnAngle > 0)
                camera.HorizontalTurnAngle = shp.CameraHorizontalTurnAngle;
            if (shp.CameraVerticalTurnUpAngle > 0)
                camera.VerticalTurnUpAngle = shp.CameraVerticalTurnUpAngle;
            if (shp.CameraVerticalTurnDownAngle > 0)
                camera.VerticalTurnDownAngle = shp.CameraVerticalTurnDownAngle;
            camera.Reset();

            sysrender = new SystemRenderer(camera, Game.GameData, Game.ResourceManager, Game);
            world = new GameWorld(sysrender);
            world.LoadSystem(sys, Game.ResourceManager);
            session.WorldReady();
            player.World = world;
            world.Objects.Add(player);
            world.RenderUpdate += World_RenderUpdate;
            world.PhysicsUpdate += World_PhysicsUpdate;
            player.Register(world.Physics);
            Game.Sound.PlayMusic(sys.MusicSpace);
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
            Game.Sound.ResetListenerVelocity();
            FadeIn(0.5, 0.5);
        }
        [MoonSharp.Interpreter.MoonSharpUserData]
        public class LuaAPI
        {
            SpaceGameplay g;
            public LuaAPI(SpaceGameplay gameplay)
            {
                this.g = gameplay;   
            }
            public Maneuver[] GetManeuvers()
            {
                return g.Game.GameData.GetManeuvers().ToArray();
            }

            public void PopulateNavmap(Navmap nav)
            {
                nav.PopulateIcons(g.ui, g.sys);
            }

            public ChatSource GetChats() => g.session.Chats;
            public double GetCredits() => g.session.Credits;


            private string activeManeuver = "FreeFlight";
            public string GetActiveManeuver() => activeManeuver;
            public LuaCompatibleDictionary GetManeuversEnabled()
            {
                var dict = new LuaCompatibleDictionary();
                dict.Set("FreeFlight", true);
                dict.Set("Goto", g.selected != null);
                dict.Set("Dock", g.selected?.GetComponent<DockComponent>() != null);
                dict.Set("Formation", false);
                return dict;
            }
            public void HotspotPressed(string e)
            {
                if (g.ManeuverSelect(e))
                {
                    activeManeuver = e;
                }
            }

            public void TextEntered(string text)
            {
                g.Hud_OnTextEntry(text);
            }

            internal void SetManeuver(string m)
            {
                activeManeuver = m;
            }
            public int ThrustPercent() => ((int)(g.powerCore.CurrentThrustCapacity / g.powerCore.Equip.ThrustCapacity * 100));
            public int Speed() => ((int)g.player.PhysicsComponent.Body.LinearVelocity.Length());
        }
		void World_MessageBroadcasted(GameObject sender, GameMessageKind kind)
		{
			switch (kind)
			{
				case GameMessageKind.ManeuverFinished:
                    uiApi.SetManeuver("FreeFlight");
					break;
			}
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
				//session.JumpTo(action.Target, action.Exit);
			}
		}

		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
            Game.Mouse.MouseDown -= Mouse_MouseDown;
			input?.Dispose();
			sysrender?.Dispose();
            world?.Dispose();
		}

		void Keyboard_KeyDown(KeyEventArgs e)
		{
            if (ui.KeyboardGrabbed)
            {
                ui.OnKeyDown(e.Key);
            }
            else
            {
                if(e.Key == Keys.Enter) ui.ChatboxEvent();
            }
        }

		void Game_TextInput(string text)
		{
			ui.OnTextEntry(text);
		}

		bool dogoto = false;
		AutopilotComponent pilotcomponent = null;
		void Hud_OnTextEntry(string obj)
        {
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

		void World_RenderUpdate(double delta)
		{

		}
        public override void OnResize()
        {
            camera.Viewport = Game.Viewport;
        }

        public bool ShowHud = true;

        public override void Update(double delta)
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
            session.GameplayUpdate(this);
            if (session.Update()) return;
            if (ShowHud && (Thn == null || !Thn.Running))
                ui.Update(Game);
            if(ui.KeyboardGrabbed)
                Game.EnableTextInput();
            else
                Game.DisableTextInput();
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

		void World_PhysicsUpdate(double delta)
		{
			control.EngineState = cruise ? EngineStates.Cruise : EngineStates.Standard;
            if(Thn == null || !Thn.Running)
			    ProcessInput(delta);
#if false
            pyw.PlotPoint(0, control.PlayerPitch);
            pyw.PlotPoint(1, control.PlayerYaw);
            pyw.PlotPoint(2, control.Roll);
#endif
            //Has to be here or glitches
            camera.ChasePosition = player.PhysicsComponent.Body.Position;
            camera.ChaseOrientation = player.PhysicsComponent.Body.Transform.ClearTranslation();
            camera.Update(delta);
            if ((Thn == null || !Thn.Running)) //HACK: Cutscene also updates the listener so we don't do it if one is running
                Game.Sound.UpdateListener(delta, camera.Position, camera.CameraForward, camera.CameraUp);
        }

		bool mouseFlight = false;

		void Input_ToggleActivated(int id)
		{
			if (ui.KeyboardGrabbed) return;
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
            if (ui.KeyboardGrabbed) return;
            if (obj == InputAction.ID_THRUST)
				control.ThrustEnabled = false;
		}

        double lastDown = -1000;

        void Mouse_MouseDown(MouseEventArgs e)
        {
            if((e.Buttons & MouseButtons.Left) > 0)
            {
                lastDown = Game.TotalTime;
            }
        }


		const float ACCEL = 85;
		GameObject selected;
		void ProcessInput(double delta)
		{
			input.Update();

			if (!ui.KeyboardGrabbed)
            {
				if (input.ActionDown(InputAction.ID_THROTTLEUP))
				{
                    shipInput.Throttle += (float)(delta);
					shipInput.Throttle = MathHelper.Clamp(shipInput.Throttle, 0, 1);
				}

				else if (input.ActionDown(InputAction.ID_THROTTLEDOWN))
				{
                    shipInput.Throttle -= (float)(delta);
                    shipInput.Throttle = MathHelper.Clamp(shipInput.Throttle, 0, 1);
				}
			}

			StrafeControls strafe = StrafeControls.None;
            if (!ui.KeyboardGrabbed)
			{
				if (input.ActionDown(InputAction.ID_STRAFELEFT)) strafe |= StrafeControls.Left;
				if (input.ActionDown(InputAction.ID_STRAFERIGHT)) strafe |= StrafeControls.Right;
				if (input.ActionDown(InputAction.ID_STRAFEUP)) strafe |= StrafeControls.Up;
				if (input.ActionDown(InputAction.ID_STRAFEDOWN)) strafe |= StrafeControls.Down;
            }

			var pc = player.PhysicsComponent;
            shipInput.Viewport = new Vector2(Game.Width, Game.Height);
            shipInput.Camera = camera;
            if (Game.Mouse.IsButtonDown(MouseButtons.Left) || mouseFlight)
			{
                var mX = Game.Mouse.X;
                var mY = Game.Mouse.Y;
                camera.MousePosition = new Vector2(
                    mX, Game.Height - mY
                );
                shipInput.MouseFlight = true;
                shipInput.MousePosition = new Vector2(mX, mY);
                camera.MouseFlight = true;
            }
			else
			{
                shipInput.MouseFlight = false;
                camera.MouseFlight = false;
			}
			control.CurrentStrafe = strafe;
			//control.EnginePower = Velocity / MAX_VELOCITY;
			var obj = GetSelection(Game.Mouse.X, Game.Mouse.Y);
			current_cur = obj == null ? cur_arrow : cur_reticle;
			
            var dir = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 1f), camera.Projection, camera.View, new Vector2(Game.Width, Game.Height));
            var start = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 0), camera.Projection, camera.View, new Vector2(Game.Width, Game.Height));
            var tgt = start + (dir.Normalized() * 400);
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

			var start = Vector3Ex.UnProject(new Vector3(x, y, 0f), camera.Projection, camera.View, vp);
			var end = Vector3Ex.UnProject(new Vector3(x, y, 1f), camera.Projection, camera.View, vp);
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
						var nd = Vector3.DistanceSquared(rb.Position, camera.Position);
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
						var nd = Vector3.DistanceSquared(rb.Position, camera.Position);
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
								
								var nd = Vector3.DistanceSquared(rb.Position, camera.Position);
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
			var dist = Vector3.DistanceSquared(rayOrigin, centre);
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

		//RigidBody debugDrawBody;
		public override void Draw(double delta)
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

            if ((Thn == null || !Thn.Running) && ShowHud)
            {
                ui.Visible = true;
                ui.RenderWidget(delta);
            }
            else
            {
                ui.Visible = false;
            }

            Game.Renderer2D.Start(Game.Width, Game.Height);
            if (Thn != null && Thn.Running)
            {
                var pct = Cutscene.LETTERBOX_HEIGHT;
                int h = (int) (Game.Height * pct);
                Game.Renderer2D.FillRectangle(new Rectangle(0, 0, Game.Width, h), Color4.Black);
                Game.Renderer2D.FillRectangle(new Rectangle(0, Game.Height - h, Game.Width, h), Color4.Black);
            }
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
                DebugDrawing.DrawShadowedText(Game.Renderer2D,  string.Format(DEMO_TEXT, camera.Position.X, camera.Position.Y, camera.Position.Z, sys.Nickname, sys.Name, DebugDrawing.SizeSuffix(GC.GetTotalMemory(false)), Velocity, sel_obj, control.PlayerPitch, control.PlayerYaw, control.Roll, mouseFlight), 5, 5);
                //pyw.Draw(Game.Renderer2D);
                current_cur.Draw(Game.Renderer2D, Game.Mouse);
            }
            DoFade(delta);
			Game.Renderer2D.Finish();
		}

        public override void Exiting()
        {
            session.OnExit();
        }

    }
}
