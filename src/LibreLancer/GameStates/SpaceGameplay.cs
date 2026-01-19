// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Client;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Infocards;
using LibreLancer.Input;
using LibreLancer.Interface;
using LibreLancer.Net;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;
using LibreLancer.Server.Components;
using LibreLancer.Sounds.VoiceLines;
using LibreLancer.Thn;
using AnmScript = LibreLancer.Utf.Anm.Script;
using LibreLancer.World;
using LibreLancer.World.Components;
using WattleScript.Interpreter;

namespace LibreLancer
{
	public class SpaceGameplay : GameState
    {
        const string DEBUG_TEXT =
@"{3} ({4})
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
		StarSystem sys;
		public GameWorld world;
        public FreelancerGame FlGame => Game;

		SystemRenderer sysrender;
		bool wireframe = false;
		bool textEntry = false;
		string currentText = "";
		public GameObject player;
		ShipPhysicsComponent control;
        ShipSteeringComponent steering;
        ShipInputComponent shipInput;
        WeaponControlComponent weapons;
		PowerCoreComponent powerCore;
        CHealthComponent playerHealth;
        public DirectiveRunnerComponent Directives;
        public SelectedTargetComponent Selection;
        private ContactList contactList;

		public float Velocity = 0f;
		const float MAX_VELOCITY = 80f;
        Cursor cur_arrow;
		Cursor cur_cross;
		Cursor cur_reticle;
		Cursor current_cur;
		CGameSession session;
        CPlayerCargoComponent cargo;
        bool loading = true;
        LoadingScreen loader;
        public Cutscene Thn;
        private UiContext ui;
        private UiWidget widget;
        private LuaAPI uiApi;

        private bool pausemenu = false;
        private bool paused = false;
        private int nextObjectiveUpdate = 0;

		public SpaceGameplay(FreelancerGame g, CGameSession session) : base(g)
        {
			FLLog.Info("Game", "Entering system " + session.PlayerSystem);
            g.ResourceManager.ClearTextures(); //Do before loading things
            g.ResourceManager.ClearMeshes();
            Game.Ui.MeshDisposeVersion++;
            this.session = session;
            sys = g.GameData.Items.Systems.Get(session.PlayerSystem);
            ui = Game.Ui;
            ui.GameApi = uiApi = new LuaAPI(this);
            uiApi.IndicatorLayer.OnRender += IndicatorLayerOnRender;
            nextObjectiveUpdate = session.CurrentObjective.Ids;
            session.ObjectiveUpdated = () => nextObjectiveUpdate = session.CurrentObjective.Ids;
            session.OnUpdateInventory = session.OnUpdatePlayerShip = null; //we should clear these handlers better
            loader = new LoadingScreen(g, g.GameData.LoadSystemResources(sys));
            loader.Init();
        }



        ChaseCamera _chaseCamera;
        TurretViewCamera _turretViewCamera;
        ICamera activeCamera;
        private bool isTurretView = false;

        void FinishLoad()
        {
            Game.Saves.Selected = -1;
            //Set up player object + camera
            player = new GameObject(session.PlayerShip, Game.ResourceManager, true, true);
            player.Nickname = "player";
            player.NetID = session.PlayerNetID;
            control = new ShipPhysicsComponent(player) {Ship = session.PlayerShip};
            shipInput = new ShipInputComponent(player) {BankLimit = session.PlayerShip.MaxBankAngle};
            weapons = new WeaponControlComponent(player);
            pilotcomponent = new AutopilotComponent(player) { LocalPlayer = true };
            steering = new ShipSteeringComponent(player);
            Selection = new SelectedTargetComponent(player);
            Directives = new DirectiveRunnerComponent(player);
            player.AddComponent(Selection);
            //Order components in terms of inputs (very important)
            player.AddComponent(pilotcomponent);
            player.AddComponent(shipInput);
            //takes input from pilot and shipinput
            player.AddComponent(steering);
            //takes input from steering
            player.AddComponent(control);
            player.AddComponent(weapons);
            player.AddComponent(new CExplosionComponent(player, session.PlayerShip.Explosion));
            cargo = new CPlayerCargoComponent(player, session);
            player.AddComponent(cargo);
            player.AddComponent(Directives);
            FLLog.Debug("Client", $"Spawning self with rotation {session.PlayerOrientation}");
            player.SetLocalTransform(new Transform3D(session.PlayerPosition, session.PlayerOrientation));
            playerHealth = new CHealthComponent(player);
            playerHealth.MaxHealth = session.PlayerShip.Hitpoints;
            playerHealth.CurrentHealth = session.PlayerShip.Hitpoints;
            player.AddComponent(playerHealth);
            player.AddComponent(new CLocalPlayerComponent(player, session));
            player.Flags |= GameObjectFlags.Player;
            if(session.PlayerShip.Mass < 0)
            {
                FLLog.Error("Ship", "Mass < 0");
            }

            player.Tag = GameObject.ClientPlayerTag;
            foreach (var equipment in session.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
            {
                EquipmentObjectManager.InstantiateEquipment(player, Game.ResourceManager, Game.Sound, EquipmentType.LocalPlayer, equipment.Hardpoint, equipment.Equipment);
            }
            powerCore = player.GetComponent<PowerCoreComponent>();
            if (powerCore == null) throw new Exception("Player launched without a powercore equipped!");
            _chaseCamera = new ChaseCamera(Game.RenderContext.CurrentViewport, Game.GameData.Items.Ini.Cameras);
            _turretViewCamera = new TurretViewCamera(Game.RenderContext.CurrentViewport, Game.GameData.Items.Ini.Cameras);
            _turretViewCamera.CameraOffset = new Vector3(0, 0, session.PlayerShip.ChaseOffset.Length());
            _chaseCamera.ChasePosition = session.PlayerPosition;
            _chaseCamera.ChaseOrientation = Matrix4x4.CreateFromQuaternion(player.LocalTransform.Orientation);
            var offset = session.PlayerShip.ChaseOffset;

            _chaseCamera.DesiredPositionOffset = offset;
            if (session.PlayerShip.CameraHorizontalTurnAngle > 0)
                _chaseCamera.HorizontalTurnAngle = session.PlayerShip.CameraHorizontalTurnAngle;
            if (session.PlayerShip.CameraVerticalTurnUpAngle > 0)
                _chaseCamera.VerticalTurnUpAngle = session.PlayerShip.CameraVerticalTurnUpAngle;
            if (session.PlayerShip.CameraVerticalTurnDownAngle > 0)
                _chaseCamera.VerticalTurnDownAngle = session.PlayerShip.CameraVerticalTurnDownAngle;
            _chaseCamera.Reset();

            activeCamera = _chaseCamera;

            sysrender = new SystemRenderer(_chaseCamera, Game.ResourceManager, Game);
            sysrender.ZOverride = true; //Draw all with regular Z
            world = new GameWorld(sysrender, Game.ResourceManager, () => session.WorldTime);
            //Game.GameData.PreloadObjects(session.Preloads);
            world.LoadSystem(sys, Game.ResourceManager, Game.Sound, false);
            session.WorldReady();
            player.World = world;
            world.AddObject(player);
            player.Register(world.Physics);
            world.Projectiles.Player = player; //For sending projectile spawns over the network
            cur_arrow = Game.ResourceManager.GetCursor("arrow");
            cur_cross = Game.ResourceManager.GetCursor("cross");
            cur_reticle = Game.ResourceManager.GetCursor("fire_neutral");
            current_cur = cur_cross;
            Game.Keyboard.TextInput += Game_TextInput;
            Game.Keyboard.KeyDown += Keyboard_KeyDown;
            Game.Mouse.MouseDown += Mouse_MouseDown;
            Game.Mouse.MouseUp += Mouse_MouseUp;
            player.World = world;
            pilotcomponent.BehaviorChanged += BehaviorChanged;
            Game.Sound.ResetListenerVelocity();
            contactList = new ContactList(this);
            ui.OpenScene("hud");
            FadeIn(0.5, 0.5);
            updateStartDelay = 3;
        }

        public override void OnSettingsChanged() =>
            sysrender.Settings = Game.Config.Settings;



        bool CanRecharge()
        {
            var first = cargo.FirstOf<ShieldBatteryEquipment>();
            if (first == null)
                return false;
            var shield = player.GetFirstChildComponent<CShieldComponent>();
            if (shield == null)
                return false;
            if (shield.Equip.Def.MaxCapacity - shield.Health < 100)
                return false;
            return true;
        }
        void UseShieldBatteries()
        {
            if (CanRecharge())
            {
                session.SpaceRpc.UseShieldBatteries();
                Game.Sound.PlayOneShot("ui_use_shield_battery_success");
            }
            else
            {
                Game.Sound.PlayOneShot("ui_use_shield_battery_failure");
            }
        }

        bool CanRepair()
        {
            var first = cargo.FirstOf<RepairKitEquipment>();
            if (first == null)
                return false;
            if (playerHealth.MaxHealth - playerHealth.CurrentHealth < 100)
                return false;
            return true;
        }

        void UseRepairKits()
        {
            if (CanRepair())
            {
                session.SpaceRpc.UseRepairKits();
                Game.Sound.PlayOneShot("ui_use_nanobots_success");
            }
            else
            {
                Game.Sound.PlayOneShot("ui_use_nanobots_failure");
            }
        }

        protected override void OnActionDown(InputAction obj)
        {
            if (!ui.KeyboardGrabbed)
            {
                switch (obj)
                {
                    case InputAction.USER_MANEUVER_DOCK:
                        ManeuverSelect("Dock");
                        break;
                    case InputAction.USER_MANEUVER_GOTO:
                        ManeuverSelect("Goto");
                        break;
                    case InputAction.USER_SCREEN_SHOT:
                        Game.Screenshots.TakeScreenshot();
                        break;
                    case InputAction.USER_FULLSCREEN:
                        Game.SetFullScreen(!Game.IsFullScreen);
                        break;
                    case InputAction.USER_REPAIR_HEALTH:
                        UseRepairKits();
                        break;
                    case InputAction.USER_REPAIR_SHIELD:
                        UseShieldBatteries();
                        break;
                    case InputAction.USER_CHAT:
                        ui.ChatboxEvent();
                        break;
                    case InputAction.USER_TRACTOR_BEAM:
                    {
                        TractorSelected();
                        break;
                    }
                    case InputAction.USER_COLLECT_LOOT:
                    {
                        TractorAll();
                        break;
                    }
                }
            }
        }

        RepAttitude GetRepToPlayer(GameObject obj)
        {
            if ((obj.Flags & GameObjectFlags.Friendly) == GameObjectFlags.Friendly) return RepAttitude.Friendly;
            if ((obj.Flags & GameObjectFlags.Neutral) == GameObjectFlags.Neutral) return RepAttitude.Neutral;
            if ((obj.Flags & GameObjectFlags.Hostile) == GameObjectFlags.Hostile) return RepAttitude.Hostile;
            if (obj.SystemObject != null)
            {
                var rep = session.PlayerReputations.GetReputation(obj.SystemObject.Reputation);
                if (rep <= Faction.HostileThreshold) return RepAttitude.Hostile;
                if (rep >= Faction.FriendlyThreshold) return RepAttitude.Friendly;
            }
            return RepAttitude.Neutral;
        }

        private int updateStartDelay = -1;

        [WattleScript.Interpreter.WattleScriptUserData]

        public class ContactList : IContactListData
        {
            readonly record struct Contact(GameObject obj, float distance, string display);

            private Contact[] Contacts = Array.Empty<Contact>();
            private SpaceGameplay game;
            private Vector3 playerPos;
            private Func<GameObject, bool> contactFilter;

            public ContactList()
            {
                contactFilter = AllFilter;
            }

            string GetDistanceString(float distance)
            {
                if (distance < 1000)
                    return $"{(int)distance}m";
                else if (distance < 10000)
                    return string.Format("{0:F1}k", distance / 1000);
                else if (distance < 90000)
                    return $"{((int) distance) / 1000}k";
                else
                    return "FAR";
            }


            Contact GetContact(GameObject obj)
            {
                var distance = Vector3.Distance(playerPos, obj.WorldTransform.Position);
                var name = obj.Name.GetName(game.Game.GameData, playerPos);
                if (obj.Kind == GameObjectKind.Ship &&
                    obj.TryGetComponent<CFactionComponent>(out var fac))
                {
                    var fn = game.Game.GameData.GetString(fac.Faction.IdsShortName);
                    if(!string.IsNullOrWhiteSpace(fn))
                        name = $"{fn} - {name}";
                }
                return new Contact(obj, distance, $"{GetDistanceString(distance)} - {name}");
            }


            bool AllFilter(GameObject o) => true;
            bool ShipFilter(GameObject o) => o.Kind == GameObjectKind.Ship;
            bool StationFilter(GameObject o) => o.Kind == GameObjectKind.Solar;

            bool LootFilter(GameObject o) => o.Kind == GameObjectKind.Loot;

            bool ImportantFilter(GameObject o)
            {
                return game.Selection.Selected == o ||
                       (o.Flags & GameObjectFlags.Important) == GameObjectFlags.Important ||
                       o.Kind == GameObjectKind.Waypoint ||
                       game.GetRepToPlayer(o) == RepAttitude.Hostile;
            }

            public void SetFilter(string filter)
            {
                contactFilter = AllFilter;
                switch (filter)
                {
                    case "important":
                        contactFilter = ImportantFilter;
                        break;
                    case "ship":
                        contactFilter = ShipFilter;
                        break;
                    case "station":
                        contactFilter = StationFilter;
                        break;
                    case "loot":
                        contactFilter = LootFilter;
                        break;
                    case "all":
                        break;
                    default:
                        FLLog.Warning("Ui", $"Unknown contact list filter {filter}, defaulting to all");
                        break;
                }
            }

            public void UpdateList()
            {
                playerPos = game.player.WorldTransform.Position;
                Contacts = game.world.Objects.Where(x => x != game.player &&
                                                         (x.Kind == GameObjectKind.Ship ||
                                                           x.Kind == GameObjectKind.Solar ||
                                                         x.Kind == GameObjectKind.Waypoint ||
                                                           x.Kind == GameObjectKind.Loot) &&
                                                         !string.IsNullOrWhiteSpace(x.Name?.GetName(game.Game.GameData, Vector3.Zero)))
                    .Where(contactFilter)
                    .Select(GetContact)
                    .OrderBy(x => x.distance).ToArray();
            }

            public ContactList(SpaceGameplay game)
            {
                this.game = game;
            }

            public int Count => Contacts.Length;
            public bool IsSelected(int index)
            {
                return game.Selection.Selected == Contacts[index].obj;
            }

            public void SelectIndex(int index)
            {
                game.Selection.Selected = Contacts[index].obj;
            }

            public string Get(int index)
            {
                return Contacts[index].display;
            }

            public RepAttitude GetAttitude(int index)
            {
                return game.GetRepToPlayer(Contacts[index].obj);
            }

            public bool IsWaypoint(int index)
            {
                return Contacts[index].obj.Kind == GameObjectKind.Waypoint;
            }

        }

        public class WidgetTemplate
        {
            public UiWidget Template;
            public Closure Callback;

            public WidgetTemplate(UiWidget template, Closure cb)
            {
                Template = template;
                Callback = cb;
            }

            public void Draw(UiContext context, RectangleF parentRectangle, float x, float y, params object[] args)
            {
                Callback?.Call(args);
                Template.X = x;
                Template.Y = y;
                Template.Render(context, parentRectangle);
            }
        }


        private int frameCount = 0;

        [WattleScriptUserData]
        public class LuaAPI
        {
            SpaceGameplay g;
            public CallbackWidget IndicatorLayer;

            public LuaAPI(SpaceGameplay gameplay)
            {
                this.g = gameplay;
                IndicatorLayer = new CallbackWidget();
            }

            private Container lastContainer;

            public void SetIndicatorLayer(Container container)
            {
                if (lastContainer != null)
                    lastContainer.Children.Remove(IndicatorLayer);
                container.Children.Add(IndicatorLayer);
            }

            public UIInventoryItem[] GetScannedInventory(string filter) => g.session.GetScannedInventory(filter);

            public Infocard GetScannedShipInfocard()
            {
                if (g.Selection.Selected == null) return null;
                if (g.Selection.Selected.TryGetComponent<ShipComponent>(out var ship))
                {
                    return g.Game.GameData.GetInfocard(ship.Ship.IdsInfo, g.Game.Fonts);
                }
                return null;
            }

            public bool CanScanSelected()
            {
                if (g.Selection.Selected == null)
                    return false;
                return g.scanner.CanScan(g.Selection.Selected);
            }

            public void ScanSelected() => g.session.SpaceRpc.Scan(g.Selection.Selected);

            public void StopScan() => g.session.SpaceRpc.StopScan();

            public Closure ScanHandler;
            public void OnUpdateScannedInventory(Closure handler)
            {
                ScanHandler = handler;
            }

            public int CurrentRank => g.session.CurrentRank;
            public double NetWorth => (double)g.session.NetWorth;
            public double NextLevelWorth => (double)g.session.NextLevelWorth;
            public PlayerStats Statistics => g.session.Statistics;
            public double CharacterPlayTime => g.session.CharacterPlayTime;

            [WattleScriptHidden] public WidgetTemplate Reticle;
            [WattleScriptHidden] public WidgetTemplate UnselectedArrow;
            [WattleScriptHidden] public WidgetTemplate SelectedArrow;
            [WattleScriptHidden] public int ShieldBatteries;
            [WattleScriptHidden] public int RepairKits;

            public int ShieldBatteryCount() => ShieldBatteries;

            public int RepairKitCount() => RepairKits;

            public void UseRepairKits() => g.UseRepairKits();

            public void UseShieldBatteries() => g.UseShieldBatteries();

            public bool CanTractorAll() => g.canTractorAll;

            public bool CanTractorSelected()
            {
                return g.canTractorAny && g.Selection.Selected != null &&
                       g.Selection.Selected.Kind == GameObjectKind.Loot &&
                       Vector3.Distance(g.Selection.Selected.WorldTransform.Position, g.tractorOrigin) <
                       g.maxTractorDistance;
            }

            public void TractorSelected() => g.TractorSelected();

            public void TractorAll() => g.TractorAll();


            public void SetReticleTemplate(UiWidget template, Closure callback) =>
                Reticle = new(template, callback);

            public void SetUnselectedArrowTemplate(UiWidget template, Closure callback) =>
                UnselectedArrow = new(template, callback);

            public void SetSelectedArrowTemplate(UiWidget template, Closure callback) =>
                SelectedArrow = new(template, callback);

            public ContactList GetContactList() => g.contactList;
            public KeyMapTable GetKeyMap()
            {
                var table = new KeyMapTable(g.Game.InputMap, g.Game.GameData.Items.Ini.Infocards);
                table.OnCaptureInput += (k) =>
                {
                    g.Input.KeyCapture = k;
                };
                return table;
            }
            public GameSettings GetCurrentSettings() => g.Game.Config.Settings.MakeCopy();

            public int GetObjectiveStrid() => g.session.CurrentObjective.Ids;
            public void ApplySettings(GameSettings settings)
            {
                g.Game.Config.Settings = settings;
                g.Game.Config.Save();
            }

            public void Respawn()
            {
                g.session.RpcServer.Respawn();
            }

            public void PopupFinish(string id)
            {
                g.waitObjectiveFrames = 30;
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
                    var embeddedServer =
                        new EmbeddedServer(g.Game.GameData, g.Game.ResourceManager, g.Game.GetSaveFolder());
                    var session = new CGameSession(g.Game, embeddedServer);
                    embeddedServer.StartFromSave(g.Game.Saves.SelectedFile, File.ReadAllBytes(g.Game.Saves.SelectedFile));
                    g.Game.ChangeState(new NetWaitState(session, g.Game));
                });
            }

            public bool CanLoadAutoSave() => !string.IsNullOrWhiteSpace(g.session.AutoSavePath);

            public void LoadAutoSave()
            {
                var path = g.session.AutoSavePath;
                g.FadeOut(0.2, () =>
                {
                    g.session.OnExit();
                    var embeddedServer =
                        new EmbeddedServer(g.Game.GameData, g.Game.ResourceManager, g.Game.GetSaveFolder());
                    var session = new CGameSession(g.Game, embeddedServer);
                    embeddedServer.StartFromSave(path, File.ReadAllBytes(path));
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

            public DisplayFaction[] GetPlayerRelations() => g.session.GetUIRelations();

            public void QuitToMenu()
            {
                g.session.QuitToMenu();
            }
            public Maneuver[] GetManeuvers()
            {
                return g.Game.GameData.GetManeuvers().ToArray();
            }

            public Infocard CurrentInfocard()
            {
                if (g.Selection.Selected?.SystemObject != null)
                {
                    int ids = g.Selection.Selected.SystemObject.IdsInfo;
                    return g.Game.GameData.GetInfocard(ids, g.Game.Fonts);
                }
                return null;
            }

            public string CurrentInfoString() => g.Selection.Selected?.Name?.GetName(g.Game.GameData, Vector3.Zero);

            public string SelectionName()
            {
                return g.Selection.Selected?.Name?.GetName(g.Game.GameData, g.player.PhysicsComponent.Body.Position) ?? "NULL";
            }

            public TargetShipWireframe SelectionWireframe() => g.Selection.Selected != null ? g.targetWireframe : null;

            public bool SelectionVisible()
            {
                return g.Selection.Selected != null && g.ScreenPosition(g.Selection.Selected).visible;
            }

            public float SelectionHealth()
            {
                if (g.Selection.Selected == null) return -1;
                if (!g.Selection.Selected.TryGetComponent<CHealthComponent>(out var health))
                    return -1;
                return MathHelper.Clamp(health.CurrentHealth / health.MaxHealth, 0, 1);
            }

            public float SelectionShield()
            {
                if (g.Selection.Selected == null) return -1;
                CShieldComponent shield;
                if ((shield = g.Selection.Selected.GetFirstChildComponent<CShieldComponent>()) == null) return -1;
                return shield.ShieldPercent;
            }

            public string SelectionReputation()
            {
                if (g.Selection.Selected == null)
                    return "neutral";
                var rep = g.GetRepToPlayer(g.Selection.Selected);
                return rep switch
                {
                    RepAttitude.Friendly => "friendly",
                    RepAttitude.Hostile => "hostile",
                    _ => "neutral"
                };
            }


            public Vector2 SelectionPosition()
            {
                if (g.Selection.Selected == null) return new Vector2(-1000, -1000);
                var (pos, _) = g.ScreenPosition(g.Selection.Selected);
                return new Vector2(
                    g.ui.PixelsToPoints(pos.X),
                    g.ui.PixelsToPoints(pos.Y)
                );
            }

            public void PopulateNavmap(Navmap nav)
            {
                nav.PopulateIcons(g.ui, g.sys);
                nav.SetVisitFunction(g.session.IsVisited);
            }

            public ChatSource GetChats() => g.session.Chats;
            public double GetCredits() => g.session.Credits;

            public float GetPlayerHealth() => g.playerHealth.CurrentHealth / g.playerHealth.MaxHealth;

            public float GetPlayerShield()
            {
                return g.player.GetFirstChildComponent<CShieldComponent>()?.ShieldPercent ?? -1;
            }

            public float GetPlayerPower() =>  g.powerCore.CurrentEnergy / g.powerCore.Equip.Capacity;

            private string activeManeuver = "FreeFlight";

            public string GetActiveManeuver() => g.pilotcomponent.CurrentBehavior switch
            {
                AutopilotBehaviors.Dock => "Dock",
                AutopilotBehaviors.Formation => "Formation",
                AutopilotBehaviors.Goto => "Goto",
                _ => "FreeFlight"
            };

            public LuaCompatibleDictionary GetManeuversEnabled()
            {
                var dict = new LuaCompatibleDictionary();
                dict.Set("FreeFlight", true);
                dict.Set("Goto", g.Selection.Selected != null);
                dict.Set("Dock", g.Selection.Selected?.GetComponent<DockInfoComponent>() != null &&
                                 g.session.DockAllowed(g.Selection.Selected));
                dict.Set("Formation", g.Selection.Selected != null && g.Selection.Selected.Kind == GameObjectKind.Ship);
                return dict;
            }

            public void HotspotPressed(string e)
            {
                g.ManeuverSelect(e);
            }

            public void ChatEntered(ChatCategory category, string text)
            {
                g.session.OnChat(category, text);
            }

            public UiEquippedWeapon[] GetWeapons() => g.weapons.GetUiElements().ToArray();

            internal void SetManeuver(string m)
            {
                activeManeuver = m;
            }
            public int ThrustPercent() => ((int)(g.powerCore.CurrentThrustCapacity / g.powerCore.Equip.ThrustCapacity * 100));
            public int Speed() => ((int)g.player.PhysicsComponent.Body.LinearVelocity.Length());
        }

        private void BehaviorChanged(AutopilotBehaviors newBehavior, AutopilotBehaviors oldBehavior)
        {
            FLLog.Debug("Player", $"Behavior swap new: {newBehavior} old: {oldBehavior}");
            uiApi.SetManeuver(newBehavior switch
            {
                AutopilotBehaviors.Dock => "Dock",
                AutopilotBehaviors.Formation => "Formation",
                AutopilotBehaviors.Goto => "Goto",
                _ => "FreeFlight"
            });
            if (newBehavior != AutopilotBehaviors.Formation &&
                player.Formation != null &&
                player.Formation.LeadShip != player)
            {
                session.SpaceRpc.LeaveFormation();
            }
            if (oldBehavior == AutopilotBehaviors.Undock)
            {
                shipInput.Throttle = 1;
            }
        }

        private DockCameraInfo dockCameraInfo = null;
        public void SetDockCam(DockCameraInfo info)
        {
            this.dockCameraInfo = info;
        }


        protected override void OnUnload()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
            Game.Mouse.MouseDown -= Mouse_MouseDown;
			sysrender?.Dispose();
            world?.Dispose();
		}

		void Keyboard_KeyDown(KeyEventArgs e)
		{
            if (ui.KeyboardGrabbed)
            {
                ui.OnKeyDown(e.Key, (e.Modifiers & KeyModifiers.Control) != 0);
            }
            else if (e.Key == Keys.Escape && ui.WantsEscape())
            {
                ui.OnEscapePressed();
            }
            else
            {
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
		public AutopilotComponent pilotcomponent = null;

        bool ManeuverSelect(string e)
		{
			switch (e)
			{
				case "FreeFlight":
                    pilotcomponent.Cancel();
					return true;
				case "Dock":
					if (Selection.Selected == null) return false;
					DockInfoComponent d;
					if ((d = Selection.Selected.GetComponent<DockInfoComponent>()) != null)
                    {
                        if (!session.DockAllowed(Selection.Selected))
                        {
                            Game.Sound.PlayVoiceLine(VoiceLines.NnVoiceName, VoiceLines.NnVoice.DockingNotAllowed);
                            return false;
                        }
                        pilotcomponent.StartDock(Selection.Selected, GotoKind.Goto);
                        session.SpaceRpc.RequestDock(Selection.Selected);
						return true;
					}
					return false;
				case "Goto":
					if (Selection.Selected == null) return false;
                    pilotcomponent.GotoObject(Selection.Selected, GotoKind.Goto);
					return true;
                case "Formation":
                    session.SpaceRpc.EnterFormation(Selection.Selected.NetID);
                    return true;
			}
			return false;
		}


        public bool ShowHud = true;
        //Set to true when the mission system selection.Selected music on launch
        public bool RtcMusic = false;
        private bool musicTriggered = false;


        private double accum = 0;
        void TimeDilatedUpdate(double delta)
        {
            accum += delta;
            double updateInterval = 1 / 60.0 * session.AdjustedInterval;
            while (accum >= updateInterval)
            {
                accum -= updateInterval;
                double FixedDelta = 1 / 60.0;

                world.Update(paused ? 0 : FixedDelta);
                if (session.Update()) return;
                if (updateStartDelay == 0)
                {
                    session.GameplayUpdate(this, FixedDelta);

                    if (isLeftDown)
                    {
                        leftDownTimer -= FixedDelta;
                    }

                    if (!musicTriggered)
                    {
                        if (!RtcMusic) Game.Sound.PlayMusic(sys.MusicSpace, 0);
                        musicTriggered = true;
                    }
                }
            }
            var fraction = accum / updateInterval;

            world.UpdateInterpolation((float)fraction);
            UpdateCamera(delta);
        }

        private LookAtCamera undockCamera = new()
        {
            ZRange = new(3f, 10000000f),
            GameFOV = true
        };

        ICamera GetCurrentCamera()
        {
            if (Thn != null && Thn.Running)
            {
                return Thn.CameraHandle;
            }
            else if (dockCameraInfo != null && pilotcomponent.CurrentBehavior == AutopilotBehaviors.Undock)
            {
                return undockCamera;
            }
            else
            {
                return activeCamera;
            }
        }
        bool IsSpecialCamera() => GetCurrentCamera() != activeCamera;

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

            if (ShowHud && !IsSpecialCamera())
            {
                contactList.UpdateList();
                uiApi.ShieldBatteries =
                    session.Items.FirstOrDefault(x => x.Equipment is ShieldBatteryEquipment)?.Count ?? 0;
                uiApi.RepairKits =
                    session.Items.FirstOrDefault(x => x.Equipment is RepairKitEquipment)?.Count ?? 0;
            }
            ui.Update(Game);
            Game.TextInputEnabled = ui.KeyboardGrabbed;
            TimeDilatedUpdate(delta);
            sysrender.Camera = GetCurrentCamera();
            if (frameCount < 2)
            {
                frameCount++;
                if (frameCount == 2) {
                    session.BeginUpdateProcess();
                }
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
            if (Selection.Selected != null && !Selection.Selected.Flags.HasFlag(GameObjectFlags.Exists)) Selection.Selected = null; //Object has been blown up/despawned
            // do tractor beam things
            if (player.TryGetComponent<CTractorComponent>(out var tractor))
            {
                tractorOrigin = tractor.WorldOrigin;
                maxTractorDistance = tractor.Equipment.Def.MaxLength;
                canTractorAny = true;
                if (tractor.BeamCount > 0)
                {
                    canTractorAll = false;
                }
                else
                {
                    canTractorAll = world.SpatialLookup.GetNearbyObjects(player, tractorOrigin, maxTractorDistance)
                        .Any(x => x.Kind == GameObjectKind.Loot);
                }
            }
            else
            {
                canTractorAny = false;
                canTractorAll = false;
            }
            // query scanner
            player.TryGetComponent<ScannerComponent>(out scanner);
		}

        void TractorSelected()
        {
            if (!canTractorAny)
            {
                return;
            }
            session.SpaceRpc.Tractor(Selection.Selected);
        }

        void TractorAll()
        {
            if (!canTractorAll)
            {
                return;
            }
            foreach (var obj in world.SpatialLookup
                         .GetNearbyObjects(player, tractorOrigin, maxTractorDistance)
                         .Where(x => x.Kind == GameObjectKind.Loot))
            {
                session.SpaceRpc.Tractor(obj);
            }
        }

        private ScannerComponent scanner;
        private Vector3 tractorOrigin;
        private bool canTractorAny;
        private bool canTractorAll;
        private float maxTractorDistance;

		bool thrust = false;

		void UpdateCamera(double delta)
        {
            activeCamera = isTurretView ? _turretViewCamera : _chaseCamera;
            _chaseCamera.Viewport = Game.RenderContext.CurrentViewport;
            _turretViewCamera.Viewport = Game.RenderContext.CurrentViewport;
            if(!IsSpecialCamera())
			    ProcessInput(delta);
            else if (ui.HasModal)
            {
                current_cur = cur_arrow;
            }

            //Has to be here or glitches
            if (!Dead)
            {
                if (dockCameraInfo != null && pilotcomponent.CurrentBehavior == AutopilotBehaviors.Undock)
                {
                    var tr = dockCameraInfo.DockHardpoint.Transform * dockCameraInfo.Parent.WorldTransform;
                    undockCamera.Update(Game.Width, Game.Height, tr.Position, player.LocalTransform.Position);
                }
                _turretViewCamera.ChasePosition = player.LocalTransform.Position;
                _chaseCamera.ChasePosition = player.LocalTransform.Position;
                _chaseCamera.ChaseOrientation = Matrix4x4.CreateFromQuaternion(player.LocalTransform.Orientation);
            }

            _turretViewCamera.Update(delta);
            _chaseCamera.Update(delta);
            if ((Thn == null ||
                 !Thn.Running)) //HACK: Cutscene also updates the listener so we don't do it if one is running
            {
                Game.Sound.UpdateListener(delta, _chaseCamera.Position, _chaseCamera.CameraForward, _chaseCamera.CameraUp);
            }
            else
            {
                Thn.Update(paused ? 0 : delta);
                ((ThnCamera)Thn.CameraHandle).DefaultZ(); //using Thn Z here is just asking for trouble
            }
        }

		bool mouseFlight = false;

        protected override void OnActionUp(InputAction action)
        {
			if (ui.KeyboardGrabbed || paused) return;
			switch (action)
			{
				case InputAction.USER_CRUISE:
                    steering.Cruise = !steering.Cruise;
					break;
				case InputAction.USER_TURN_SHIP:
					mouseFlight = !mouseFlight;
					break;
                case InputAction.USER_AUTO_TURRET:
                    isTurretView = !isTurretView;
                    break;
            }
		}

        private bool isLeftDown = false;
        private double leftDownTimer = 0;

        void Mouse_MouseDown(MouseEventArgs e)
        {
            if((e.Buttons & MouseButtons.Left) > 0)
            {
                if(!(Game.Debug.CaptureMouse) && !ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y))
                {
                    var newSelection = world.GetSelection(activeCamera, player, Game.Mouse.X, Game.Mouse.Y, Game.Width, Game.Height);
                    if (newSelection != null) Selection.Selected = newSelection;

                    if (!isLeftDown)
                    {
                        isLeftDown = true;
                        leftDownTimer = 0.25;
                    }
                }
                else
                {
                    isLeftDown = false;
                    leftDownTimer = 0;
                }
            }
        }

        private void Mouse_MouseUp(MouseEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Left) > 0)
            {
                leftDownTimer = 0;
                isLeftDown = false;
            }
        }

        public bool Dead = false;
        public void Killed()
        {
            Dead = true;
            Explode(player);
            world.RemoveObject(player);
            ui.Event("Killed");
        }

        public void StoryFail(int failIds)
        {
            Dead = true;
            ui.Event("Killed", failIds);
        }

        public void Explode(GameObject obj)
        {
            if (obj.TryGetComponent<CExplosionComponent>(out var df) &&
                df.Explosion?.Effect != null)
            {
                var pfx = df.Explosion.Effect.GetEffect(FlGame.ResourceManager);
                sysrender.SpawnTempFx(pfx, obj.WorldTransform.Position);
            }
        }

        public void StopShip()
        {
            shipInput.Throttle = 0;
            shipInput.AutopilotThrottle = 0;
            shipInput.MouseFlight = false;
            _chaseCamera.MouseFlight = false;
            _turretViewCamera.PanControls = Vector2.Zero;
            pilotcomponent.Cancel();
            steering.Thrust = false;
            shipInput.Reverse = false;
            steering.Cruise = false;
        }

        bool GetCrosshair(out Vector2 screenPos, out Vector3 worldPos)
        {
            screenPos = Vector2.Zero;
            worldPos = Vector3.Zero;
            if (Selection.Selected?.PhysicsComponent == null)
                return false;
            if (Selection.Selected.Kind != GameObjectKind.Ship ||
                (Selection.Selected.Flags & GameObjectFlags.Exists) != GameObjectFlags.Exists)
                return false;
            var myPos = player.PhysicsComponent.Body.Position;
            var myVel = player.PhysicsComponent.Body.LinearVelocity;
            var otherPos = Selection.Selected.PhysicsComponent.Body.Position;
            var otherVel = Selection.Selected.PhysicsComponent.Body.LinearVelocity;
            var speed = weapons.GetAverageGunSpeed();
            Aiming.GetTargetLeading(otherPos - myPos, otherVel - myVel, speed, out var t);
            worldPos = (otherPos + otherVel * t);
            bool vis;
            (screenPos, vis) = ScreenPosition(worldPos);
            return vis;
        }

        private bool crosshairHit = false;
        Vector3 GetAimPoint()
        {
            crosshairHit = false;
            var m = new Vector2(Game.Mouse.X, Game.Mouse.Y);
            if (GetCrosshair(out var crosshairScreen, out var crosshairAim))
            {
                if (Vector2.Distance(m, crosshairScreen) < CrosshairSize())
                {
                    crosshairHit = true;
                    return crosshairAim;
                }
            }
            GetCameraMatrices(out var cameraView, out var cameraProjection);

            var end = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 1f), cameraProjection, cameraView, new Vector2(Game.Width, Game.Height));
            var start = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 0), cameraProjection, cameraView, new Vector2(Game.Width, Game.Height));
            var dir = (end - start).Normalized();
            var tgt = start + (dir * 400);

            if (world.Physics.PointRaycast(player.PhysicsComponent.Body, start, dir, 1000, out var contactPoint, out var po)) {
                return contactPoint;
            }
            return tgt;
        }

		void ProcessInput(double delta)
        {
            if (Dead) {
                current_cur = cur_arrow;
                return;
            }
            if (paused) return;
            Input.Update();

            if (!ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y))
            {
                shipInput.Throttle = MathHelper.Clamp(shipInput.Throttle + (Game.Mouse.Wheel / 3f), 0, 1);
            }

            shipInput.Reverse = false;

			if (!ui.KeyboardGrabbed)
            {
				if (Input.IsActionDown(InputAction.USER_INC_THROTTLE))
				{
                    shipInput.Throttle += (float)(delta);
					shipInput.Throttle = MathHelper.Clamp(shipInput.Throttle, 0, 1);
				}

				else if (Input.IsActionDown(InputAction.USER_DEC_THROTTLE))
				{
                    shipInput.Throttle -= (float)(delta);
                    shipInput.Throttle = MathHelper.Clamp(shipInput.Throttle, 0, 1);
				}
                steering.Thrust = Input.IsActionDown(InputAction.USER_AFTERBURN);
                shipInput.Reverse = Input.IsActionDown(InputAction.USER_MANEUVER_BRAKE_REVERSE);
            }

			StrafeControls strafe = StrafeControls.None;
            if (!ui.KeyboardGrabbed)
			{
				if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_LEFT)) strafe |= StrafeControls.Left;
				if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_RIGHT)) strafe |= StrafeControls.Right;
				if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_UP)) strafe |= StrafeControls.Up;
				if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_DOWN)) strafe |= StrafeControls.Down;
            }

			var pc = player.PhysicsComponent;
            shipInput.Viewport = new Vector2(Game.Width, Game.Height);
            shipInput.Camera = _chaseCamera;
            if (((isLeftDown && leftDownTimer < 0) || mouseFlight) && control.Active)
			{
                var mX = Game.Mouse.X;
                var mY = Game.Mouse.Y;
                if (isTurretView)
                {
                    _turretViewCamera.PanControls = new Vector2(
                        2f * (mX / (float)Game.Width) -1f, -(2f * (mY / (float)Game.Height) - 1f)
                    );
                    shipInput.MouseFlight = false;
                    _chaseCamera.MouseFlight = false;
                }
                else
                {
                    _chaseCamera.MousePosition = new Vector2(
                        mX, Game.Height - mY
                    );
                    shipInput.MouseFlight = true;
                    shipInput.MousePosition = new Vector2(mX, mY);
                    _chaseCamera.MouseFlight = true;
                    _turretViewCamera.PanControls = Vector2.Zero;
                }
            }
			else
			{
                shipInput.MouseFlight = false;
                _chaseCamera.MouseFlight = false;
                _turretViewCamera.PanControls = Vector2.Zero;
            }
			control.CurrentStrafe = strafe;


            var obj = world.GetSelection(activeCamera, player, Game.Mouse.X, Game.Mouse.Y, Game.Width, Game.Height);
            if (ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y))
                current_cur = cur_arrow;
            else {
                current_cur = obj == null ? cur_cross : cur_reticle;
            }

            weapons.AimPoint = GetAimPoint();


            if (Input.IsActionDown(InputAction.USER_FIRE_WEAPONS))
                weapons.FireAll();
            if(Input.IsActionDown(InputAction.USER_LAUNCH_MISSILES))
                weapons.FireMissiles();
            for (int i = 0; i < 10; i++)
            {
                if (Input.IsActionDown(InputAction.USER_FIRE_WEAPON1 + i))
                    weapons.FireIndex(i);
            }

            if (world.Projectiles.HasMissilesQueued)
            {
                session.SpaceRpc.FireMissiles(world.Projectiles.GetMissileQueue());
            }
        }

        public void ClearComm()
        {
            ui.Event("Comm", new object[] { null });
        }

        public void ClearScan()
        {
            uiApi.ScanHandler?.Call(false);
        }

        public void UpdateScan()
        {
            uiApi.ScanHandler?.Call(true);
        }

        public void OpenComm(GameObject obj, string voice)
        {
            if (!obj.TryGetComponent<CostumeComponent>(out var costume)) {
                ClearComm();
                return;
            }
            if (!Game.GameData.Items.Ini.Voices.Voices.TryGetValue(voice, out var voiceData)) {
                ClearComm();
                return;
            }
            var scripts = new List<AnmScript>();
            var canim = Game.GameData.GetCharacterAnimations();
            foreach (var s in voiceData.Scripts)
            {
                if(canim.Scripts.TryGetValue(s, out var sc))
                    scripts.Add(sc);
            }

            Accessory acc = costume.Helmet;
            RigidModel accModel = null;
            if (acc != null)
            {
                accModel = (costume.Helmet?.ModelFile.LoadFile(Game.ResourceManager).Drawable as IRigidModelFile)
                    ?.CreateRigidModel(true, Game.ResourceManager);
            }
            var app = new CommAppearance()
            {
                Head = costume.Head?.LoadModel(Game.ResourceManager),
                Body = costume.Body?.LoadModel(Game.ResourceManager),
                Accessory = acc,
                AccessoryModel = accModel,
                Male = string.Equals(costume.Body?.Sex, "male", StringComparison.OrdinalIgnoreCase),
                Scripts = scripts
            };
            string factionName = null;
            if (obj.TryGetComponent<CFactionComponent>(out var fac) &&
                fac?.Faction != null)
            {
                factionName = Game.GameData.GetString(fac.Faction.IdsName);
            }
            ui.Event("Comm", new CommData()
            {
                Source = obj.Name.GetName(Game.GameData, Vector3.Zero),
                Affiliation = factionName,
                Appearance = app
            });
        }

        public void StartTradelane()
        {
            player.GetComponent<ShipPhysicsComponent>().Active = false;
            player.GetComponent<WeaponControlComponent>().Enabled = false;
            pilotcomponent.Cancel();
        }

        public void TradelaneDisrupted()
        {
            Game.Sound.PlayVoiceLine(VoiceLines.NnVoiceName, VoiceLines.NnVoice.TradeLaneDisrupted);
            EndTradelane();
        }

        public void EndTradelane()
        {
            player.GetComponent<ShipPhysicsComponent>().Active = true;
            player.GetComponent<WeaponControlComponent>().Enabled = true;
        }


        void GetCameraMatrices(out Matrix4x4 view, out Matrix4x4 projection)
        {
            view = activeCamera.View;
            projection = activeCamera.Projection;
        }

        void GetViewProjection(out Matrix4x4 vp)
        {
            vp = activeCamera.ViewProjection;
        }

        (Vector2 pos, bool visible) ScreenPosition(Vector3 worldPos)
        {
            GetViewProjection(out var vp);
            var clipSpace = Vector4.Transform(new Vector4(worldPos, 1), vp);
            var ndc = clipSpace / clipSpace.W;
            var viewSize = new Vector2(Game.Width, Game.Height);
            var windowSpace = new Vector2(
                ((ndc.X + 1.0f) / 2.0f) * Game.Width,
                ((1.0f - ndc.Y) / 2.0f) * Game.Height
            );
            bool visible =
                windowSpace.X >= 0 &&
                windowSpace.X <= Game.Width &&
                windowSpace.Y >= 0 &&
                windowSpace.Y <= Game.Height;
            if (clipSpace.Z < 0)
                windowSpace *= -1;
            return (windowSpace, visible && ndc.Z < 1);
        }

        (Vector2 pos, bool visible) ScreenPosition(GameObject obj)
        {
            return ScreenPosition(obj.WorldTransform.Position);
        }

        private GameObject missionWaypoint;

        void UpdateObjectiveObjects()
        {
            if (missionWaypoint != null)
            {
                if (Selection.Selected == missionWaypoint)
                    Selection.Selected = null;
                world.RemoveObject(missionWaypoint);
                missionWaypoint = null;
            }
            if ((session.CurrentObjective.Kind == ObjectiveKind.Object ||
                session.CurrentObjective.Kind == ObjectiveKind.NavMarker) &&
                sys.Nickname.Equals(session.CurrentObjective.System, StringComparison.OrdinalIgnoreCase))
            {

                var pos = session.CurrentObjective.Kind == ObjectiveKind.Object
                    ? (world.GetObject(session.CurrentObjective.Object)?.WorldTransform ?? Transform3D.Identity).Position
                    : session.CurrentObjective.Position;
                if (pos != Vector3.Zero)
                {
                    var waypointArch = Game.GameData.Items.Archetypes.Get("waypoint");
                    missionWaypoint = new GameObject(waypointArch, null, Game.ResourceManager);
                    missionWaypoint.Name = new ObjectName(1091); //Mission Waypoint
                    missionWaypoint.SetLocalTransform(new Transform3D(pos, Quaternion.Identity));
                    missionWaypoint.World = world;
                    world.AddObject(missionWaypoint);
                    missionWaypoint.Register(world.Physics);
                }
            }
        }

        private TargetShipWireframe targetWireframe = new TargetShipWireframe();

        int CrosshairSize()
        {
            float size = 14;
            float ratio = (Game.Height / 480f);
            return (int)(size * ratio);
        }

        private bool showObjectList = false;

		//RigidBody debugDrawBody;
        private int waitObjectiveFrames = 120;
		public override unsafe void Draw(double delta)
		{
            RenderMaterial.VertexLighting = false;
            if (loading)
            {
                loader.Draw(delta);
                return;
            }

            if (Thn != null && Thn.Running)
            {
                //Viewport FOV calculations unaffected by letterboxing
                Game.RenderContext.ClearColor = Color4.Black;
                Game.RenderContext.ClearAll();
                var newRatio = ((double)Game.Width / Game.Height) * 1.39;
                var newHeight = Game.Width / newRatio;
                var diff = (Game.Height - newHeight);
                var vp = Game.RenderContext.CurrentViewport;
                vp.Y = (int)(vp.Y + (diff / 2));
                vp.Height = (int)(vp.Height - (diff));
                Game.RenderContext.PushViewport(vp.X, vp.Y, vp.Width, vp.Height);
                Thn.UpdateViewport(Game.RenderContext.CurrentViewport, (float)Game.Width / Game.Height);
            }

            if (Selection.Selected != null) {
                targetWireframe.Model = Selection.Selected.Model.RigidModel;
                var lookAt = Matrix4x4.CreateLookAt(player.LocalTransform.Position,
                    Vector3.Transform(Vector3.UnitZ * 4, player.LocalTransform.Matrix()), Vector3.UnitY);

                targetWireframe.Matrix = (lookAt * Selection.Selected.LocalTransform.Matrix()).ClearTranslation();
            }

            if (updateStartDelay > 0)
            {
                updateStartDelay--;
                if (updateStartDelay == 0)
                    session.UpdateStart(this);
            }
            if (waitObjectiveFrames > 0) waitObjectiveFrames--;
            world.RenderUpdate(delta);
            sysrender.DebugRenderer.StartFrame(Game.RenderContext);

            sysrender.Draw(Game.RenderContext.CurrentViewport.Width, Game.RenderContext.CurrentViewport.Height);

            sysrender.DebugRenderer.Render();

            if (GetCrosshair(out var crosshairScreen, out _))
            {
                var sz = CrosshairSize();
                var r0 = new Rectangle((int)(crosshairScreen.X - (sz / 2)), (int)crosshairScreen.Y, sz, 1);
                var r1 = new Rectangle((int)crosshairScreen.X, (int)crosshairScreen.Y - (sz / 2), 1, sz);
                Game.RenderContext.Renderer2D.FillRectangle(r0, Color4.Red);
                Game.RenderContext.Renderer2D.FillRectangle(r1, Color4.Red);
            }

            if (!IsSpecialCamera() && ShowHud)
            {
                ui.Visible = true;
                if (nextObjectiveUpdate != 0 && waitObjectiveFrames <= 0)
                {
                    ui.Event("ObjectiveUpdate", nextObjectiveUpdate);
                    nextObjectiveUpdate = 0;
                    UpdateObjectiveObjects();
                }
            }
            else
            {
                ui.Visible = false;
            }

            if (Thn != null && Thn.Running)
            {
                Game.RenderContext.PopViewport();
            }
            ui.RenderWidget(delta);
            session.SetDebug(Game.Debug.Enabled);
            Game.Debug.Draw(delta, () =>
            {
                ImGui.Checkbox("Object List", ref showObjectList);
                ImGui.Text($"Object Count: {world.Objects.Count}");
                string sel_obj = "None";
                if (Selection.Selected != null)
                {
                    if (Selection.Selected.Name == null)
                        sel_obj = "unknown object";
                    else
                        sel_obj = Selection.Selected.Name?.GetName(Game.GameData, player.PhysicsComponent.Body.Position) ?? "unknown object";
                    sel_obj = $"{sel_obj} ({Selection.Selected.Nickname ?? "null nickname"})";
                }
                var systemName = Game.GameData.GetString(sys.IdsName);
                var text = string.Format(DEBUG_TEXT, activeCamera.Position.X, activeCamera.Position.Y, activeCamera.Position.Z,
                    sys.Nickname, systemName, DebugDrawing.SizeSuffix(GC.GetTotalMemory(false)), Velocity, sel_obj,
                    control.Steering.X, control.Steering.Y, control.Steering.Z, mouseFlight, session.WorldTime);
                ImGui.Text(text);
                ImGui.Text($"crosshairHit: {crosshairHit}");
                var dbgT = session.GetSelectedDebugInfo();
                if(!string.IsNullOrWhiteSpace(dbgT))
                    ImGui.Text(dbgT);
                if (Selection.Selected?.PhysicsComponent?.Body?.Collider is ConvexMeshCollider cvx)
                {
                    ImGui.Text($"selected compound children: {cvx.BepuChildCount}");
                }
                ImGui.Text($"input queue: {session.UpdateQueueCount}");
                ImGui.Text($"tick offset: {session.LastTickOffset}");
                ImGui.Text($"dropped inputs: {session.DroppedInputs}");
                ImGui.Text($"average tick offset: {session.AverageTickOffset}");
                ImGui.Text($"interval: {session.AdjustedInterval}");
                ImGui.Text($"Client Tick: {session.WorldTick}");
                if (session.Multiplayer)
                {
                    var floats = new float[session.UpdatePacketSizes.Count];
                    for (int i = 0; i < session.UpdatePacketSizes.Count; i++)
                        floats[i] = session.UpdatePacketSizes[i];
                    fixed (float* f = floats)
                    {
                        if (floats.Length > 0)
                        {
                            ImGui.Text($"last ack received: {session.Acks.Tick}");
                            ImGui.Text($"update packet size: {floats[floats.Length - 1]}");
                            ImGui.PlotLines("update packet size", ref floats[0], floats.Length);
                        }
                    }
                }
                else
                {
                    ImGui.Text($"Server Tick: {session.EmbeddedServer.Server.CurrentTick}");
                }

                bool hasDebug = world.Physics.DebugRenderer != null;
                ImGui.Checkbox("Draw hitboxes", ref hasDebug);
                ImGui.BeginDisabled(!hasDebug);
                ImGui.Checkbox("Draw raycasts", ref world.Physics.ShowRaycasts);
                ImGui.EndDisabled();
                if (hasDebug)
                    world.Physics.DebugRenderer = sysrender.DebugRenderer;
                else
                    world.Physics.DebugRenderer = null;
                ImGui.Text($"Free Audio Voices: {Game.Audio.FreeSources}");
                ImGui.Text($"Playing Sounds: {Game.Audio.PlayingInstances}");
                ImGui.Text($"Audio Update Time: {Game.Audio.UpdateTime:0.000}ms");
                if (!session.Multiplayer)
                {
                    ImGui.Text($"Storyline: {session.EmbeddedServer.Server.LocalPlayer.Story?.CurrentStory?.Nickname}");
                }
                //ImGuiNET.ImGui.Text(pilotcomponent.ThrottleControl.Current.ToString());
            }, () =>
            {
                Game.Debug.MissionWindow(session.GetTriggerInfo());
                if(showObjectList)
                    Game.Debug.ObjectsWindow(world.Objects);
            });
            if ((!IsSpecialCamera() && ShowHud) || Game.Debug.Enabled || ui.HasModal)
            {
                current_cur.Draw(Game.RenderContext.Renderer2D, Game.Mouse, Game.TotalTime);
            }
            DoFade(delta);
		}

        (Vector2, float) ArrowPosition(Vector2 pos)
        {
            var screenCenter = new Vector2(ui.ScreenWidth, 480) / 2f;
            pos -= screenCenter;

            var angle = -(MathF.PI / 2) - MathF.Atan2(pos.Y, -pos.X);

            var cos = MathF.Cos(angle);
            var sin = -MathF.Sin(angle);
            var m = cos / sin;
            var screenBounds = screenCenter * 0.9f;

            if(cos > 0) {
                pos = new Vector2(screenBounds.Y/m, screenBounds.Y);
            } else {
                pos = new Vector2(-screenBounds.Y/m, -screenBounds.Y);
            }

            if(pos.X > screenBounds.X) {
                pos = new Vector2(screenBounds.X, screenBounds.X * m);
            } else if (pos.X < -screenBounds.X) {
                pos = new Vector2(-screenBounds.X, -screenBounds.X * m);
            }

            pos = -pos;
            pos += screenCenter;

            return (pos, angle);
        }

        void DrawSelectedArrow(GameObject obj, Vector2 pos, UiContext context, RectangleF parentRectangle)
        {
            var rep = GetRepToPlayer(obj) switch
            {
                RepAttitude.Friendly => "friendly",
                RepAttitude.Hostile => "hostile",
                _ => "neutral"
            };
            var (arrowPos, angle) = ArrowPosition(pos);
            uiApi.SelectedArrow?.Draw(
                context, parentRectangle, arrowPos.X, arrowPos.Y,
                angle, rep, (obj.Flags & GameObjectFlags.Important) != 0
            );
        }

        void DrawUnselectedArrow(GameObject obj, Vector2 pos, UiContext context, RectangleF parentRectangle)
        {
            var rep = GetRepToPlayer(obj) switch
            {
                RepAttitude.Friendly => "friendly",
                RepAttitude.Hostile => "hostile",
                _ => "neutral"
            };
            var (arrowPos, angle) = ArrowPosition(pos);
            uiApi.UnselectedArrow?.Draw(
                context, parentRectangle, arrowPos.X, arrowPos.Y,
                angle, rep, 0.5f, (obj.Flags & GameObjectFlags.Important) != 0
                );
        }

        void DrawShipReticle(GameObject obj, Vector2 pos, UiContext context, RectangleF parentRectangle)
        {
           // var rep = GetRepToPlayer(obj);

        }

        void IndicatorLayerOnRender(UiContext context, RectangleF parentRectangle)
        {
            foreach (var obj in world.Objects) {
                if (obj == Selection.Selected)
                { //Draw last
                }
                else if (obj.Kind == GameObjectKind.Ship)
                {
                    var (pos, visible) = ScreenPosition(obj);
                    if (!visible && (
                        (obj.Flags & GameObjectFlags.Hostile) == GameObjectFlags.Hostile ||
                        (obj.Flags & GameObjectFlags.Important) == GameObjectFlags.Important))
                        DrawUnselectedArrow(obj, pos, context, parentRectangle);
                    if(visible)
                        DrawShipReticle(obj, pos, context, parentRectangle);
                }
                else if ((obj.Flags & GameObjectFlags.Hostile) == GameObjectFlags.Hostile ||
                         (obj.Flags & GameObjectFlags.Important) == GameObjectFlags.Important)
                {
                    var (pos, visible) = ScreenPosition(obj);
                    if (!visible)
                        DrawUnselectedArrow(obj, pos, context, parentRectangle);
                }
            }

            if (Selection.Selected != null)
            {
                var (pos, visible) = ScreenPosition(Selection.Selected);
                if (!visible) {
                    DrawSelectedArrow(Selection.Selected, pos, context, parentRectangle);
                }
            }
        }

        public override void Exiting()
        {
            session.OnExit();
        }

    }
}
