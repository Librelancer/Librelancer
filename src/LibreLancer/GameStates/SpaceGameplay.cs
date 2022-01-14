// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using LibreLancer.Interface;
using LibreLancer.Net;
using LibreLancer.Physics;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

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
World Time: {12:F2}
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
        CHealthComponent playerHealth;
        
		public float Velocity = 0f;
		const float MAX_VELOCITY = 80f;
        Cursor cur_arrow;
		Cursor cur_cross;
		Cursor cur_reticle;
		Cursor current_cur;
		InputManager input;
		CGameSession session;
        bool loading = true;
        LoadingScreen loader;
        public Cutscene Thn;
        private UiContext ui;
        private UiWidget widget;
        private LuaAPI uiApi;

        private bool pausemenu = false;
        private bool paused = false;

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
            Game.Saves.Selected = -1;
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
            player.Components.Add(new CDamageFuseComponent(player, shp.Fuses));

            player.SetLocalTransform(session.PlayerOrientation * Matrix4x4.CreateTranslation(session.PlayerPosition));
            player.PhysicsComponent.Mass = shp.Mass;
            playerHealth = new CHealthComponent(player);
            playerHealth.MaxHealth = shp.Hitpoints;
            playerHealth.CurrentHealth = shp.Hitpoints;
            player.Components.Add(playerHealth);
            if(shp.Mass < 0)
            {
                FLLog.Error("Ship", "Mass < 0");
            }

            player.Tag = GameObject.ClientPlayerTag;
            foreach (var equipment in session.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
            {
                EquipmentObjectManager.InstantiateEquipment(player, Game.ResourceManager, EquipmentType.LocalPlayer, equipment.Hardpoint, equipment.Equipment);
            }
            powerCore = player.GetComponent<PowerCoreComponent>();
            if (powerCore == null) throw new Exception("Player launched without a powercore equipped!");
            camera = new ChaseCamera(Game.RenderContext.CurrentViewport, Game.GameData.Ini.Cameras);
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
            sysrender.ZOverride = true; //Draw all with regular Z
            world = new GameWorld(sysrender);
            world.LoadSystem(sys, Game.ResourceManager, false, session.SpawnTime);
            session.WorldReady();
            player.World = world;
            world.AddObject(player);
            world.RenderUpdate += World_RenderUpdate;
            world.PhysicsUpdate += World_PhysicsUpdate;
            player.Register(world.Physics);
            Game.Sound.PlayMusic(sys.MusicSpace);
            //world.Physics.EnableWireframes(debugphysics);
            cur_arrow = Game.ResourceManager.GetCursor("arrow");
            cur_cross = Game.ResourceManager.GetCursor("cross");
            cur_reticle = Game.ResourceManager.GetCursor("fire_neutral");
            current_cur = cur_cross;
            Game.Keyboard.TextInput += Game_TextInput;
            Game.Keyboard.KeyDown += Keyboard_KeyDown;
            Game.Mouse.MouseDown += Mouse_MouseDown;
            Game.Mouse.MouseUp += Mouse_MouseUp;
            input = new InputManager(Game);
            input.ToggleActivated += Input_ToggleActivated;
            input.ToggleUp += Input_ToggleUp;
            pilotcomponent = new AutopilotComponent(player);
            player.Components.Add(pilotcomponent);
            player.World = world;
            world.MessageBroadcasted += World_MessageBroadcasted;
            Game.Sound.ResetListenerVelocity();
            FadeIn(0.5, 0.5);
        }

        

        private int frameCount = 0;
        [MoonSharp.Interpreter.MoonSharpUserData]
        public class LuaAPI
        {
            SpaceGameplay g;
            public LuaAPI(SpaceGameplay gameplay)
            {
                this.g = gameplay;   
            }

            public GameSettings GetCurrentSettings() => g.Game.Config.Settings.MakeCopy();

            public void ApplySettings(GameSettings settings)
            {
                g.Game.Config.Settings = settings;
                g.Game.Config.Save();
            }

            public void PopupFinish(string id)
            {
                g.session.RpcServer.ClosedPopup(id);
                Resume();
            }

            public int CruiseCharge() => g.control.EngineState == EngineStates.CruiseCharging ? (int)(g.control.ChargePercent * 100) : -1;
            public bool IsMultiplayer() => g.session.Multiplayer;
            
            public SaveGameFolder SaveGames() => g.Game.Saves;
            public void DeleteSelectedGame() => g.Game.Saves.TryDelete(g.Game.Saves.Selected);

            public void LoadSelectedGame()
            {
                g.FadeOut(0.2, () =>
                {
                    g.session.OnExit();
                    var embeddedServer = new EmbeddedServer(g.Game.GameData);
                    var session = new CGameSession(g.Game, embeddedServer);
                    embeddedServer.StartFromSave(g.Game.Saves.SelectedFile);
                    g.Game.ChangeState(new NetWaitState(session, g.Game));
                });
            }

            public void SaveGame(string description)
            {
                g.session.Save(description);
            }
            
            public void Resume()
            {
                g.session.Resume();
                g.pausemenu = false;
                g.paused = false;
            }
            
            public void QuitToMenu()
            {
                g.session.QuitToMenu();
            }
            public Maneuver[] GetManeuvers()
            {
                return g.Game.GameData.GetManeuvers().ToArray();
            }

            public string SelectionName()
            {
                return g.selected?.Name ?? "NULL";
            }

            public bool SelectionVisible()
            {
                return g.selected != null && g.ScreenPosition(g.selected).visible;
            }

            public float SelectionHealth()
            {
                if (g.selected == null) return -1;
                if (!g.selected.TryGetComponent<CHealthComponent>(out var health))
                    return -1;
                return MathHelper.Clamp(health.CurrentHealth / health.MaxHealth, 0, 1);
            }

            public float SelectionShield()
            {
                if (g.selected == null) return -1;
                if (!g.selected.TryGetComponent<CHealthComponent>(out var health)) return -1;
                return health.ShieldHealth;
            }
            

            public LuaVector2 SelectionPosition()
            {
                if (g.selected == null) return new LuaVector2(-1000, -1000);
                var (pos, visible) = g.ScreenPosition(g.selected);
                if (visible) {
                    return new LuaVector2(
                        g.ui.PixelsToPoints(pos.X),
                        g.ui.PixelsToPoints(pos.Y)
                    );
                } else {
                    return new LuaVector2(-1000, -1000);
                }
            }

            public void PopulateNavmap(Navmap nav)
            {
                nav.PopulateIcons(g.ui, g.sys);
            }

            public ChatSource GetChats() => g.session.Chats;
            public double GetCredits() => g.session.Credits;

            public float GetPlayerHealth() => g.playerHealth.CurrentHealth / g.playerHealth.MaxHealth;
            public float GetPlayerShield() => g.playerHealth.ShieldHealth;
        
            public float GetPlayerPower() => 1f;

            private string activeManeuver = "FreeFlight";
            public string GetActiveManeuver() => activeManeuver;
            public LuaCompatibleDictionary GetManeuversEnabled()
            {
                var dict = new LuaCompatibleDictionary();
                dict.Set("FreeFlight", true);
                dict.Set("Goto", g.selected != null);
                dict.Set("Dock", g.selected?.GetComponent<CDockComponent>() != null);
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
                if (!pausemenu && e.Key == Keys.F1)
                {
                    pausemenu = true;
                    if(!session.Multiplayer) 
                        paused = true;
                    session.Pause();
                    ui.Event("Pause");
                }
                #if DEBUG
                if (e.Key == Keys.R && (e.Modifiers & KeyModifiers.Control) != 0)
                    world.RenderDebugPoints = !world.RenderDebugPoints;
                #endif
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
                    pilotcomponent.Cancel();
					return true;
				case "Dock":
					if (selected == null) return false;
					CDockComponent d;
					if ((d = selected.GetComponent<CDockComponent>()) != null)
					{
                        pilotcomponent.StartDock(selected);
                        if(d.Action.Kind != DockKinds.Tradelane)
                            session.RpcServer.RequestDock(selected.Nickname);
						return true;
					}
					return false;
				case "Goto":
					if (selected == null) return false;
                    pilotcomponent.GotoObject(selected);
					return true;
			}
			return false;
		}

		void World_RenderUpdate(double delta)
		{

		}
        public override void OnResize()
        {
            camera.Viewport = Game.RenderContext.CurrentViewport;
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
            world.Update(paused ? 0 : delta);
            if (Thn != null && Thn.Running)
            {
                sysrender.Camera = Thn.CameraHandle;
            }
            else
                sysrender.Camera = camera;
            if (frameCount < 2)
            {
                frameCount++;
                if(frameCount == 2)
                    session.BeginUpdateProcess();
            }
            else
            {
                if (session.Popups.Count > 0 && session.Popups.TryDequeue(out var popup))
                {
                    FLLog.Debug("Space", "Displaying popup");
                    if(!session.Multiplayer) 
                        paused = true;
                    session.Pause();
                    ui.Event("Popup", popup.Title, popup.Contents, popup.ID);
                }
            }
		}

		bool thrust = false;

		void World_PhysicsUpdate(double delta)
		{
            if(Thn == null || !Thn.Running)
			    ProcessInput(delta);
            //Has to be here or glitches
            camera.ChasePosition = player.PhysicsComponent.Body.Position;
            camera.ChaseOrientation = player.PhysicsComponent.Body.Transform.ClearTranslation();
            camera.Update(delta);
            if ((Thn == null || !Thn.Running)) //HACK: Cutscene also updates the listener so we don't do it if one is running
                Game.Sound.UpdateListener(delta, camera.Position, camera.CameraForward, camera.CameraUp);
            else
            {
                Thn.Update(paused ? 0 : delta);
                ((ThnCamera)Thn.CameraHandle).DefaultZ(); //using Thn Z here is just asking for trouble
            }
        }

		bool mouseFlight = false;

		void Input_ToggleActivated(int id)
		{
			if (ui.KeyboardGrabbed || paused) return;
			switch (id)
			{
				case InputAction.ID_TOGGLECRUISE:
                    control.CruiseToggle();
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
            if (ui.KeyboardGrabbed || paused) return;
            if (obj == InputAction.ID_THRUST)
				control.ThrustEnabled = false;
		}

        private bool isLeftDown = false;


        void Mouse_MouseDown(MouseEventArgs e)
        {
            if((e.Buttons & MouseButtons.Left) > 0)
            {
                if(!(Game.Debug.CaptureMouse) && !ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y))
                {
                    var newselected = GetSelection(Game.Mouse.X, Game.Mouse.Y);
                    if (newselected != null) selected = newselected;
                    isLeftDown = true;
                }
                else
                {
                    isLeftDown = false;
                }
            } 
        }

        private void Mouse_MouseUp(MouseEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Left) > 0)
            {
                isLeftDown = false;
            }
        }

		const float ACCEL = 85;
		GameObject selected;
		void ProcessInput(double delta)
        {
            if (paused) return;
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
            if (isLeftDown || mouseFlight)
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
            var obj = GetSelection(Game.Mouse.X, Game.Mouse.Y);
            if (ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y))
                current_cur = cur_arrow;
            else {
                current_cur = obj == null ? cur_cross : cur_reticle;
            }
            var end = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 1f), camera.Projection, camera.View, new Vector2(Game.Width, Game.Height));
            var start = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 0), camera.Projection, camera.View, new Vector2(Game.Width, Game.Height));
            var dir = (end - start).Normalized();
            var tgt = start + (dir * 400);
            weapons.AimPoint = tgt;

            if (world.Physics.PointRaycast(player.PhysicsComponent.Body, start, dir, 1000, out var contactPoint, out var po)) {
                weapons.AimPoint = contactPoint;
            }

           
            if (Game.Mouse.IsButtonDown(MouseButtons.Right))
                weapons.FireAll();
            if (world.Projectiles.HasQueued)
            {
                session.RpcServer.FireProjectiles(world.Projectiles.GetQueue());
            }
        }
        
        

		GameObject GetSelection(float x, float y)
		{
			var vp = new Vector2(Game.Width, Game.Height);
            var start = Vector3Ex.UnProject(new Vector3(x, y, 0f), camera.Projection, camera.View, vp);
			var end = Vector3Ex.UnProject(new Vector3(x, y, 1f), camera.Projection, camera.View, vp);
            var dir = (end - start).Normalized();

			PhysicsObject rb;

            var result = SelectionCast(
				start,
				dir,
				50000,
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
            var md2 = maxDist * maxDist;
			foreach (var rb in world.Physics.Objects)
            {
				if (rb.Tag == player) continue;
                if (Vector3.DistanceSquared(rb.Position, camera.Position) > md2) continue;
                if (rb.Collider is SphereCollider)
				{
					//Test spheres
					var sph = (SphereCollider)rb.Collider;
                    var ray = new Ray(rayOrigin, direction);
                    var sphere = new BoundingSphere(rb.Position, sph.Radius);
                    var res = ray.Intersects(sphere);
                    if (res != null)
                    {
                        var p2 = rayOrigin + (direction * res.Value);
                        if (res == 0.0) p2 = rb.Position;
                        var nd = Vector3.DistanceSquared(p2, camera.Position);
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
                    var box = rb.GetBoundingBox();
                    if (!rb.GetBoundingBox().RayIntersect(ref rayOrigin, ref jitterDir)) continue;
                    var nd = Vector3.DistanceSquared(rb.Position, camera.Position);
                    if (nd < dist)
                    {
                        dist = nd;
                        body = rb;
                    }
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
        
        (Vector2 pos, bool visible) ScreenPosition(GameObject obj)
        {
            var worldPos = Vector3.Transform(Vector3.Zero, obj.WorldTransform);
            var clipSpace = Vector4.Transform(new Vector4(worldPos, 1), camera.ViewProjection);
            var ndc = clipSpace / clipSpace.W;
            var viewSize = new Vector2(Game.Width, Game.Height);
            var windowSpace = new Vector2(
                ((ndc.X + 1.0f) / 2.0f) * Game.Width,
                ((1.0f - ndc.Y) / 2.0f) * Game.Height
            );
            return (windowSpace, ndc.Z < 1);
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

            sysrender.DebugRenderer.StartFrame(camera, Game.RenderContext);
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

            if (Thn != null && Thn.Running)
            {
                var pct = Cutscene.LETTERBOX_HEIGHT;
                int h = (int) (Game.Height * pct);
                Game.RenderContext.Renderer2D.FillRectangle(new Rectangle(0, 0, Game.Width, h), Color4.Black);
                Game.RenderContext.Renderer2D.FillRectangle(new Rectangle(0, Game.Height - h, Game.Width, h), Color4.Black);
            }
            Game.Debug.Draw(delta, () =>
            {
                string sel_obj = "None";
                if (selected != null)
                {
                    if (selected.Name == null)
                        sel_obj = "unknown object";
                    else
                        sel_obj = selected.Name;
                }
                var text = string.Format(DEMO_TEXT, camera.Position.X, camera.Position.Y, camera.Position.Z,
                    sys.Nickname, sys.Name, DebugDrawing.SizeSuffix(GC.GetTotalMemory(false)), Velocity, sel_obj,
                    control.PlayerPitch, control.PlayerYaw, control.Roll, mouseFlight, session.WorldTime);
                ImGuiNET.ImGui.Text(text);
            });
            if ((Thn == null || !Thn.Running) && ShowHud)
            {
                current_cur.Draw(Game.RenderContext.Renderer2D, Game.Mouse, Game.TotalTime);
            }
            DoFade(delta);
		}

        public override void Exiting()
        {
            session.OnExit();
        }

    }
}
