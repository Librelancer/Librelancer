// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Client;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Graphics;
using LibreLancer.Input;
using LibreLancer.Interface;
using LibreLancer.Net;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;
using LibreLancer.Sounds.VoiceLines;
using LibreLancer.Thn;
using AnmScript = LibreLancer.Utf.Anm.Script;
using LibreLancer.World;
using LibreLancer.World.Components;
using WattleScript.Interpreter;

namespace LibreLancer
{
    public partial class SpaceGameplay : GameState
    {
        private StarSystem sys;
        public GameWorld world = null!;
        public FreelancerGame FlGame => Game;

        private SystemRenderer sysrender = null!;
        public GameObject player = null!;
        private ShipPhysicsComponent control = null!;
        private ShipSteeringComponent steering = null!;
        private ShipInputComponent shipInput = null!;
        private WeaponControlComponent weapons = null!;
        private PowerCoreComponent powerCore = null!;
        private CHealthComponent playerHealth = null!;
        public DirectiveRunnerComponent Directives = null!;
        public SelectedTargetComponent Selection = null!;
        private ContactList contactList = null!;

        private ChaseCamera _chaseCamera = null!;
        private TurretViewCamera _turretViewCamera = null!;
        private ICamera activeCamera = null!;
        private bool isTurretView = false;

        public float Velocity = 0f;
        private const float CRUISE_CAMERA_LAG_SPEED_BAND = 150f;
        private const float CRUISE_CAMERA_LAG_PER_BAND = 6f;
        private const float CRUISE_CAMERA_LAG_MAX = CRUISE_CAMERA_LAG_PER_BAND * 2f;
        private const float CRUISE_CAMERA_LAG_HALFLIFE = 0.18f;
        private float cruiseCameraLag = 0f;
        private float cruiseCameraLagVelocity = 0f;
        private Cursor cur_arrow = null!;
        private Cursor cur_cross = null!;
        private Cursor cur_reticle = null!;
        private Cursor current_cur = null!;
        private CGameSession session;
        private CPlayerCargoComponent cargo = null!;
        private bool loading = true;
        private LoadingScreen? loader;
        public Cutscene? Thn;

        private bool pausemenu = false;
        private bool paused = false;
        private int nextObjectiveUpdate = 0;
        private bool objectiveObjectsDirty = true;

        private int updateStartDelay = -1;
        private double systemAnnouncementDelay = -1;
        private DockCameraInfo? dockCameraInfo = null;
        private bool fadeToRoom;
        public AutopilotComponent? pilotComponent = null;


        // Set to true when the mission system selection.Selected music on launch
        public bool RtcMusic = false;
        public bool RtcMusicOneShot = false;
        private bool musicTriggered = false;
        private ScannerComponent? scanner;
        private Vector3 tractorOrigin;
        private bool canTractorAny;
        private bool canTractorAll;
        private float maxTractorDistance;

        private bool crosshairHit = false;
        private const float UserWaypointReachDistance = 100f;
        private GameObject? missionWaypoint;
        private GameObject? userWaypoint;
        private int userWaypointCounter;
        private const float WaypointSelectionStartSize = 52f;
        private const float WaypointSelectionEndSize = 150f;
        private const double WaypointSelectionAnimationDuration = 0.18;
        private int selectedWaypointAnimationObject;
        private double selectedWaypointAnimationStart;
        private double bestPathRecheckTimer;
        private TargetShipWireframe targetWireframe = new();
        private double accum = 0;

        public bool Dead = false;
        private bool isLeftDown = false;
        private double leftDownTimer = 0;
        private bool mouseFlight = false;

        public SpaceGameplay(FreelancerGame g, CGameSession session) : base(g)
        {
            FLLog.Info("Game", "Entering system " + session.PlayerSystem);
            g.ResourceManager.ClearTextures(); // Do before loading things
            g.ResourceManager.ClearMeshes();
            Game.Ui.MeshDisposeVersion++;
            this.session = session;
            sys = g.GameData.Items.Systems.Get(session.PlayerSystem)!;
            CreateHud();
            nextObjectiveUpdate = session.CurrentObjective.Ids;
            session.ObjectiveUpdated = () =>
            {
                nextObjectiveUpdate = session.CurrentObjective.Ids;
                objectiveObjectsDirty = true;
                UpdateObjectiveRoute();
            };
            session.OnUpdateInventory = session.OnUpdatePlayerShip = null; // we should clear these handlers better
            loader = new LoadingScreen(g, g.GameData.LoadSystemResources(sys)!);
            loader.Init();
        }

        private void FinishLoad()
        {
            Game.Saves.Selected = -1;
            // Set up player object + camera
            player = new GameObject(session.PlayerShip!, Game.ResourceManager, true, true)
            {
                Nickname = "player",
                NetID = session.PlayerNetID
            };

            control = new ShipPhysicsComponent(player, session.PlayerShip!);
            shipInput = new ShipInputComponent(player) { BankLimit = session.PlayerShip!.MaxBankAngle };
            weapons = new WeaponControlComponent(player);
            pilotComponent = new AutopilotComponent(player) { LocalPlayer = true };
            steering = new ShipSteeringComponent(player);
            Selection = new SelectedTargetComponent(player);
            Directives = new DirectiveRunnerComponent(player);
            player.AddComponent(Selection);

            // Order components in terms of inputs (very important)
            player.AddComponent(pilotComponent);
            player.AddComponent(shipInput);

            // takes input from pilot and shipinput
            player.AddComponent(steering);

            // takes input from steering
            player.AddComponent(control);
            player.AddComponent(weapons);
            player.AddComponent(new CExplosionComponent(player, session.PlayerShip.Explosion!));

            cargo = new CPlayerCargoComponent(player, session);
            player.AddComponent(cargo);
            player.AddComponent(Directives);

            FLLog.Debug("Client", $"Spawning self with rotation {session.PlayerOrientation}");
            player.SetLocalTransform(new Transform3D(session.PlayerPosition, session.PlayerOrientation));
            playerHealth = new CHealthComponent(player)
            {
                MaxHealth = session.PlayerShip.Hitpoints,
                CurrentHealth = session.PlayerShip.Hitpoints
            };

            player.AddComponent(playerHealth);
            player.AddComponent(new CLocalPlayerComponent(player, session));
            player.Flags |= GameObjectFlags.Player;

            if (session.PlayerShip.Mass < 0)
            {
                FLLog.Error("Ship", "Mass < 0");
            }

            foreach (var equipment in session.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
            {
                EquipmentObjectManager.InstantiateEquipment(player, Game.ResourceManager, Game.Sound,
                    EquipmentType.LocalPlayer, equipment.Hardpoint, equipment.Equipment!);
            }

            if (!player.TryGetComponent(out powerCore!))
            {
                throw new Exception("Player launched without a powercore equipped!");
            }

            _chaseCamera = new ChaseCamera(Game.RenderContext.CurrentViewport, Game.GameData.Items.Ini.Cameras);
            _turretViewCamera =
                new TurretViewCamera(Game.RenderContext.CurrentViewport, Game.GameData.Items.Ini.Cameras)
                {
                    CameraOffset = new Vector3(0, 0, 2 * player.Model!.RigidModel.GetRadius())
                };

            _chaseCamera.ChasePosition = session.PlayerPosition;
            _chaseCamera.ChaseOrientation = Matrix4x4.CreateFromQuaternion(player.LocalTransform.Orientation);
            var offset = session.PlayerShip.ChaseOffset;

            _chaseCamera.DesiredPositionOffset = offset;

            if (session.PlayerShip.CameraHorizontalTurnAngle > 0)
            {
                _chaseCamera.HorizontalTurnAngle = session.PlayerShip.CameraHorizontalTurnAngle;
            }

            if (session.PlayerShip.CameraVerticalTurnUpAngle > 0)
            {
                _chaseCamera.VerticalTurnUpAngle = session.PlayerShip.CameraVerticalTurnUpAngle;
            }

            if (session.PlayerShip.CameraVerticalTurnDownAngle > 0)
            {
                _chaseCamera.VerticalTurnDownAngle = session.PlayerShip.CameraVerticalTurnDownAngle;
            }

            _chaseCamera.Reset();

            activeCamera = _chaseCamera;

            sysrender = new SystemRenderer(_chaseCamera, Game.ResourceManager, Game);
            sysrender.ZOverride = true; // Draw all with regular Z
            world = new GameWorld(sysrender, Game.Sound, Game.ResourceManager, () => session.WorldTime);
            // Game.GameData.PreloadObjects(session.Preloads);
            world.LoadSystem(sys, Game.ResourceManager, Game.Sound, false);
            session.WorldReady();
            world.AddObject(player);
            player.Register(world);
            world.Projectiles.Player = player; // For sending projectile spawns over the network
            RefreshActiveUserWaypoint(false);
            UpdateObjectiveRoute();
            cur_arrow = Game.ResourceManager.GetCursor("arrow")!;
            cur_cross = Game.ResourceManager.GetCursor("cross")!;
            cur_reticle = Game.ResourceManager.GetCursor("fire_neutral")!;
            current_cur = cur_cross;
            Game.Keyboard.TextInput += Game_TextInput;
            Game.Keyboard.KeyDown += Keyboard_KeyDown;
            Game.Mouse.MouseDown += Mouse_MouseDown;
            Game.Mouse.MouseUp += Mouse_MouseUp;
            pilotComponent.BehaviorChanged += BehaviorChanged;
            Game.Sound.ResetListenerVelocity();
            contactList = new ContactList(this);
            ui.OpenScene("hud");
            FadeIn(0.5, 0.5);
            if (session.ConsumeSystemEntryAnnouncement())
                systemAnnouncementDelay = 5;
            GC.Collect();
            updateStartDelay = 3;
        }

        public override void OnSettingsChanged() =>
            sysrender.Settings = Game.Config.Settings;

        private bool CanRecharge()
        {
            var first = cargo.FirstOf<ShieldBatteryEquipment>();

            if (first == null)
            {
                return false;
            }

            var shield = player.GetFirstChildComponent<CShieldComponent>();

            if (shield == null)
            {
                return false;
            }

            if (shield.Equip.Def.MaxCapacity - shield.Health < 100)
            {
                return false;
            }

            return true;
        }

        private void UseShieldBatteries()
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

        private bool CanRepair()
        {
            var first = cargo.FirstOf<RepairKitEquipment>();

            if (first == null)
            {
                return false;
            }

            if (playerHealth.MaxHealth - playerHealth.CurrentHealth < 100)
            {
                return false;
            }

            return true;
        }

        private void UseRepairKits()
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
            if (ui.KeyboardGrabbed)
            {
                return;
            }

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

        private RepAttitude GetRepToPlayer(GameObject obj)
        {
            if ((obj.Flags & GameObjectFlags.Friendly) == GameObjectFlags.Friendly)
            {
                return RepAttitude.Friendly;
            }

            if ((obj.Flags & GameObjectFlags.Neutral) == GameObjectFlags.Neutral)
            {
                return RepAttitude.Neutral;
            }

            if ((obj.Flags & GameObjectFlags.Hostile) == GameObjectFlags.Hostile)
            {
                return RepAttitude.Hostile;
            }

            if (obj.SystemObject == null)
            {
                return RepAttitude.Neutral;
            }

            var rep = session.PlayerReputations.GetReputation(obj.SystemObject.Reputation);

            return rep switch
            {
                <= Faction.HostileThreshold => RepAttitude.Hostile,
                >= Faction.FriendlyThreshold => RepAttitude.Friendly,
                _ => RepAttitude.Neutral
            };

        }

        [WattleScriptUserData]
        public class ContactList : IContactListData
        {
            private readonly record struct Contact(
                GameObject Obj,
                float Distance,
                string DistanceString,
                string Label,
                ContactIcon Icon);

            private Contact[] Contacts = [];
            private SpaceGameplay game = null!;
            private Vector3 playerPos;
            private Func<GameObject, bool> contactFilter;
            private double timer = 0;
            private GameObject? lastSelected = null;

            private const double UpdateInterval = 1.0; // 1 second

            public ContactList()
            {
                contactFilter = AllFilter;
            }

            private string FormatDistance(float distance)
            {
                if (distance < 1000)
                {
                    return $"{(int)distance}";
                }
                else if (distance < 10000)
                {
                    return $"{distance / 1000:F1}k";
                }
                else if (distance < 90000)
                {
                    return $"{((int)distance) / 1000}k";
                }
                else
                {
                    return "FAR";
                }
            }

            private Contact GetContact(GameObject obj)
            {
                var distance = Vector3.Distance(playerPos, obj.WorldTransform.Position);
                var name = obj.Name?.GetName(game.Game.GameData, playerPos);

                ContactIcon icon = ContactIcon.WeaponPlatform;
                if (obj.SystemObject != null)
                {
                    icon = obj.SystemObject.Archetype?.Type switch
                    {
                        ArchetypeType.airlock_gate => ContactIcon.Jumpgate,
                        ArchetypeType.jump_gate => ContactIcon.Jumpgate,
                        ArchetypeType.jump_hole => ContactIcon.Jumpgate,
                        ArchetypeType.jumphole => ContactIcon.Jumpgate,
                        ArchetypeType.destroyable_depot => ContactIcon.LootableDepot,
                        ArchetypeType.planet => ContactIcon.Planet,
                        ArchetypeType.tradelane_ring => ContactIcon.Tradelane,
                        ArchetypeType.station => ContactIcon.Station,
                        ArchetypeType.docking_ring => ContactIcon.Station,
                        _ => ContactIcon.WeaponPlatform
                    };
                }
                else if (obj.Kind == GameObjectKind.Waypoint)
                {
                    icon = ContactIcon.Waypoint;
                }
                else if (obj.Kind == GameObjectKind.Loot)
                {
                    icon = ContactIcon.Loot;
                }
                else if (obj.Kind == GameObjectKind.Ship)
                {
                    icon = obj.NetID > 0 ? ContactIcon.OtherPlayer : ContactIcon.Ship;
                }

                if (obj.Kind != GameObjectKind.Ship || !obj.TryGetComponent<CFactionComponent>(out var fac))
                {
                    return new Contact(obj, distance, FormatDistance(distance), name ?? "-", icon);
                }

                var fn = game.Game.GameData.GetString(fac.Faction.IdsShortName);

                if (!string.IsNullOrWhiteSpace(fn))
                {
                    name = $"{fn} - {name}";
                }

                return new Contact(obj, distance, FormatDistance(distance), name ?? "-", icon);
            }

            private bool AllFilter(GameObject o) => true;
            private bool ShipFilter(GameObject o) => o.Kind == GameObjectKind.Ship;
            private bool StationFilter(GameObject o) => o.Kind == GameObjectKind.Solar;

            private bool LootFilter(GameObject o) => o.Kind == GameObjectKind.Loot;

            private bool ImportantFilter(GameObject o)
            {
                return game.Selection.Selected == o ||
                       (o.Flags & GameObjectFlags.Important) == GameObjectFlags.Important ||
                       o.NetID > 0 || // remote player
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
                UpdateList();
            }

            public void Update(double delta)
            {
                timer -= delta;
                if (timer <= 0 ||
                    lastSelected != game.Selection.Selected ||
                    Contacts.Any(x => (x.Obj.Flags & GameObjectFlags.Exists) == 0))
                {
                    UpdateList();
                }
            }

            void UpdateList()
            {
                playerPos = game.player.WorldTransform.Position;
                Contacts = game.world.Objects.Where(x => x != game.player &&
                                                         x.Kind is GameObjectKind.Ship or GameObjectKind.Solar
                                                             or GameObjectKind.Waypoint or GameObjectKind.Loot &&
                                                         !string.IsNullOrWhiteSpace(x.Name?.GetName(game.Game.GameData,
                                                             Vector3.Zero)))
                    .Where(contactFilter)
                    .Select(GetContact)
                    .OrderBy(x => x.Distance).ToArray();
                lastSelected = game.Selection.Selected;
                timer = UpdateInterval;
            }

            public ContactList(SpaceGameplay game) : this()
            {
                this.game = game;
            }

            public int Count => Contacts.Length;

            public bool IsSelected(int index)
            {
                return game.Selection.Selected == Contacts[index].Obj;
            }

            public void SelectIndex(int index)
            {
                game.Selection.Selected = Contacts[index].Obj;
            }

            public string GetLabel(int index)
            {
                return Contacts[index].Label;
            }

            public string GetDistanceString(int index)
            {
                return Contacts[index].DistanceString;
            }

            public RepAttitude GetAttitude(int index)
            {
                return game.GetRepToPlayer(Contacts[index].Obj);
            }

            public ContactIcon GetIcon(int index) => Contacts[index].Icon;

            public bool IsWaypoint(int index)
            {
                return Contacts[index].Obj.Kind == GameObjectKind.Waypoint;
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

            public void Draw(UiContext context,
                double delta,
                DrawList2D drawList,
                RectangleF canvasRectangle,
                float x, float y, params object[] args)
            {
                Callback?.Call(args);
                Template.X = x;
                Template.Y = y;
                Template.OnLayout(context, new Layout(canvasRectangle), delta);
                Template.Update(context, delta);
                Template.Render(context, delta, drawList);
            }
        }

        private int frameCount = 0;

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
                systemAnnouncementDelay = 5;
            }
        }

        public void SetDockCam(DockCameraInfo info)
        {
            this.dockCameraInfo = info;
        }

        public void FadeToRoom(Action changeState)
        {
            FadeOut(0.5, changeState);
        }

        public bool ShouldFadeToRoom => fadeToRoom;

        protected override void OnUnload()
        {
            Game.Keyboard.TextInput -= Game_TextInput;
            Game.Keyboard.KeyDown -= Keyboard_KeyDown;
            Game.Mouse.MouseDown -= Mouse_MouseDown;
            sysrender?.Dispose();
            world?.Dispose();
        }

        private void Keyboard_KeyDown(KeyEventArgs e)
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

                    if (!session.Multiplayer)
                    {
                        paused = true;
                    }

                    session.Pause();
                    ui.Event("Pause");
                }
                if (e.Key == Keys.R && (e.Modifiers & KeyModifiers.Control) != 0)
                {
                    world.RenderAutopilotDebug = !world.RenderAutopilotDebug;
#if DEBUG
                    world.RenderDebugPoints = world.RenderAutopilotDebug;
#endif
                }
            }
        }

        private void Game_TextInput(string text)
        {
            ui.OnTextEntry(text);
        }

        private bool ManeuverSelect(string e)
        {
            if (!session.IsManeuverEnabled(e))
                return false;

            switch (e)
            {
                case "FreeFlight":
                    pilotComponent!.Cancel();
                    return true;
                case "Dock":
                    if (Selection.Selected == null)
                    {
                        return false;
                    }

                    if (!Selection.Selected.TryGetComponent<DockInfoComponent>(out var dock))
                    {
                        return false;
                    }

                    if (!session.DockAllowed(Selection.Selected))
                    {
                        Game.Sound.PlayVoiceLine(VoiceLines.NnVoiceName, VoiceLines.NnVoice.DockingNotAllowed);
                        return false;
                    }

                    pilotComponent!.StartDock(Selection.Selected, GotoKind.Goto);
                    var dockCam = dock.GetDockCamera(0);
                    if (dockCam != null)
                    {
                        SetDockCam(dockCam);
                    }
                    session.RegisterRouteDock(Selection.Selected.NicknameCRC, sys.CRC);
                    session.SpaceRpc.RequestDock(Selection.Selected);
                    return true;

                case "Goto":
                    if (Selection.Selected == null)
                    {
                        return false;
                    }

                    pilotComponent!.GotoObject(Selection.Selected, GotoKind.Goto);
                    return true;
                case "Formation":
                    session.SpaceRpc.EnterFormation(Selection.Selected!.NetID);
                    return true;
            }

            return false;
        }

        private void TimeDilatedUpdate(double delta)
        {
            accum += delta;
            double updateInterval = 1 / 60.0 * session.AdjustedInterval;

            while (accum >= updateInterval)
            {
                accum -= updateInterval;
                double FixedDelta = 1 / 60.0;

                world.Update(paused ? 0 : FixedDelta);

                if (session.Update())
                {
                    return;
                }

                if (updateStartDelay == 0)
                {
                    session.GameplayUpdate(this, FixedDelta);

                    if (isLeftDown)
                    {
                        leftDownTimer -= FixedDelta;
                    }

                    if (musicTriggered)
                    {
                        if (RtcMusicOneShot && !Game.Sound.MusicPlaying)
                        {
                            RtcMusic = false;
                            RtcMusicOneShot = false;
                            if (!string.IsNullOrWhiteSpace(sys.MusicSpace))
                                Game.Sound.PlayMusic(sys.MusicSpace!, 0);
                        }
                        continue;
                    }

                    if (!RtcMusic)
                    {
                        Game.Sound.PlayMusic(sys.MusicSpace!, 0);
                    }

                    musicTriggered = true;
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

        private ICamera GetCurrentCamera()
        {
            if (Thn != null && Thn.Running)
            {
                return Thn.CameraHandle;
            }
            else if (UseDockCamera())
            {
                return undockCamera;
            }
            else
            {
                return activeCamera;
            }
        }

        private bool UseDockCamera()
        {
            var dockCameraActive = pilotComponent?.DockCameraActive == true;
            if (dockCameraActive && pilotComponent?.CurrentBehavior == AutopilotBehaviors.Dock)
                fadeToRoom = true;

            return dockCameraInfo != null &&
                   (pilotComponent?.CurrentBehavior == AutopilotBehaviors.Undock ||
                    dockCameraActive);
        }

        private bool IsSpecialCamera() => GetCurrentCamera() != activeCamera;

        private static float HalfLifeToDamping(float halfLife) =>
            2.7725887f / (halfLife + 1e-5f);

        private static void CriticalSpringDamper(
            ref float position,
            ref float velocity,
            float goal,
            float halfLife,
            float delta)
        {
            var y = HalfLifeToDamping(halfLife) * 0.5f;
            var j0 = position - goal;
            var j1 = velocity + j0 * y;
            var eydt = (float)Math.Exp(-y * delta);
            position = eydt * (j0 + j1 * delta) + goal;
            velocity = eydt * (velocity - j1 * y * delta);
        }

        private float CalculateCruiseCameraLag(double delta)
        {
            var targetLag = 0f;
            if (!Dead && activeCamera == _chaseCamera && !IsSpecialCamera() &&
                control.EngineState == EngineStates.Cruise &&
                player.PhysicsComponent?.Body != null)
            {
                var speed = player.PhysicsComponent.Body.LinearVelocity.Length();
                var speedBand = MathHelper.Clamp(speed / CRUISE_CAMERA_LAG_SPEED_BAND, 0, 2);
                targetLag = Math.Min(CRUISE_CAMERA_LAG_MAX, speedBand * CRUISE_CAMERA_LAG_PER_BAND);
            }

            CriticalSpringDamper(
                ref cruiseCameraLag,
                ref cruiseCameraLagVelocity,
                targetLag,
                CRUISE_CAMERA_LAG_HALFLIFE,
                (float)delta);
            return cruiseCameraLag;
        }

        public override void Update(double delta)
        {
            if (loading)
            {
                if (loader!.Update(delta))
                {
                    loading = false;
                    loader = null;
                    FinishLoad();
                }

                return;
            }

            if (systemAnnouncementDelay >= 0)
            {
                systemAnnouncementDelay -= delta;
                if (systemAnnouncementDelay <= 0)
                {
                    systemAnnouncementDelay = -1;
                    Game.Typewriter.PlayString($"{Game.GameData.GetString(sys.IdsName)} SYSTEM.",
                        TypewriterStyle.LocationEntry);
                }
            }

            contactList.Update(delta);
            if (ShowHud && !IsSpecialCamera())
            {
                uiApi.ShieldBatteries =
                    session.Items.FirstOrDefault(x => x.Equipment is ShieldBatteryEquipment)?.Count ?? 0;
                uiApi.RepairKits =
                    session.Items.FirstOrDefault(x => x.Equipment is RepairKitEquipment)?.Count ?? 0;
            }

            ui.Update(Game, delta);
            Game.TextInputEnabled = ui.KeyboardGrabbed;
            TimeDilatedUpdate(delta);
            UpdateBestPathRoute(delta);
            UpdateUserWaypointRoute();
            sysrender.Camera = GetCurrentCamera();

            if (frameCount < 2)
            {
                frameCount++;

                if (frameCount == 2)
                {
                    session.BeginUpdateProcess();
                }
            }
            else
            {
                if (session.Popups.Count > 0 && session.Popups.TryDequeue(out var popup))
                {
                    FLLog.Debug("Space", "Displaying popup");

                    if (!session.Multiplayer)
                    {
                        paused = true;
                    }

                    session.Pause();
                    ui.Event("Popup", popup.Title, popup.Contents, popup.ID);
                }
            }

            if (Selection.Selected != null && !Selection.Selected.Flags.HasFlag(GameObjectFlags.Exists))
            {
                Selection.Selected = null; // Object has been blown up/despawned
            }

            DrawSelectedFormationLine();

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
            player.TryGetComponent(out scanner);
        }

        private void TractorSelected()
        {
            if (!canTractorAny)
            {
                return;
            }

            session.SpaceRpc.Tractor(Selection.Selected!);
        }

        private void TractorAll()
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

        private void UpdateCamera(double delta)
        {
            activeCamera = isTurretView ? _turretViewCamera : _chaseCamera;
            _chaseCamera.Viewport = Game.RenderContext.CurrentViewport;
            _turretViewCamera.Viewport = Game.RenderContext.CurrentViewport;

            if (!IsSpecialCamera())
            {
                ProcessInput(delta);
            }
            else if (ui.HasModal)
            {
                current_cur = cur_arrow;
            }

            // Has to be here or glitches
            if (!Dead)
            {
                if (UseDockCamera())
                {
                    var tr = dockCameraInfo.DockHardpoint.Transform * dockCameraInfo.Parent.WorldTransform;
                    undockCamera.Update(Game.Width, Game.Height, tr.Position, player.LocalTransform.Position);
                }

                _turretViewCamera.ChasePosition = player.LocalTransform.Position;
                _chaseCamera.ChaseOrientation = Matrix4x4.CreateFromQuaternion(player.LocalTransform.Orientation);
                var cruiseLag = CalculateCruiseCameraLag(delta);
                var playerForward = Vector3.Transform(-Vector3.UnitZ, player.LocalTransform.Orientation);
                _chaseCamera.ChasePosition = player.LocalTransform.Position - (playerForward * cruiseLag);
            }

            _turretViewCamera.Update(delta);
            _chaseCamera.Update(delta);

            if (Thn is not
                {
                    Running: true
                }) // HACK: Cutscene also updates the listener so we don't do it if one is running
            {
                Game.Sound.UpdateListener(delta, _chaseCamera.Position, _chaseCamera.CameraForward,
                    _chaseCamera.CameraUp);
            }
            else
            {
                Thn.Update(paused ? 0 : delta);
                ((ThnCamera)Thn.CameraHandle).DefaultZ(); // using Thn Z here is just asking for trouble
            }
        }

        protected override void OnActionUp(InputAction action)
        {
            if (ui.KeyboardGrabbed || paused)
            {
                return;
            }

            switch (action)
            {
                case InputAction.USER_CRUISE:
                    steering.Cruise = !steering.Cruise;
                    steering.EngineKill = false;
                    break;
                case InputAction.USER_TURN_SHIP:
                    mouseFlight = !mouseFlight;
                    break;
                case InputAction.USER_AUTO_TURRET:
                    isTurretView = !isTurretView;
                    break;
                case InputAction.USER_MANEUVER_ENGINEKILL:
                    steering.Cruise = false;
                    steering.EngineKill = true;
                    break;
            }
        }

        private void Mouse_MouseDown(MouseEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Left) <= 0)
            {
                return;
            }

            if (!(Game.Debug.CaptureMouse) && !ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y))
            {
                var newSelection = GetMouseSelection();

                if (newSelection != null)
                {
                    Selection.Selected = newSelection;
                }

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

        private void Mouse_MouseUp(MouseEventArgs e)
        {
            if ((e.Buttons & MouseButtons.Left) > 0)
            {
                leftDownTimer = 0;
                isLeftDown = false;
            }
        }

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
            if (!obj.TryGetComponent<CExplosionComponent>(out var df) ||
                df.Explosion?.Effect == null)
            {
                return;
            }
            world.SpawnTempFx(df.Explosion.Effect, obj.WorldTransform.Position);
        }

        public void StopShip()
        {
            shipInput.Throttle = 0;
            shipInput.AutopilotThrottle = 0;
            shipInput.MouseFlight = false;
            _chaseCamera.MouseFlight = false;
            _turretViewCamera.PanControls = Vector2.Zero;
            pilotComponent?.Cancel();
            steering.Thrust = false;
            shipInput.Reverse = false;
            steering.Cruise = false;
            steering.EngineKill = false;
        }

        private bool GetCrosshair(out Vector2 screenPos, out Vector3 worldPos)
        {
            screenPos = Vector2.Zero;
            worldPos = Vector3.Zero;

            if (Selection.Selected?.PhysicsComponent == null)
            {
                return false;
            }

            if (Selection.Selected.Kind != GameObjectKind.Ship ||
                (Selection.Selected.Flags & GameObjectFlags.Exists) != GameObjectFlags.Exists)
            {
                return false;
            }

            var myPos = player.WorldTransform.Position;
            var myVel = player.PhysicsComponent!.Body.LinearVelocity;
            var otherPos = Selection.Selected.WorldTransform.Position;
            var otherVel = Selection.Selected.PhysicsComponent.Body.LinearVelocity;
            var speed = weapons.GetAverageGunSpeed();
            Aiming.GetTargetLeading(otherPos - myPos, otherVel - myVel, speed, out var t);
            worldPos = (otherPos + otherVel * t);
            bool vis;
            (screenPos, vis) = ScreenPosition(worldPos);
            return vis;
        }

        private Vector3 GetAimPoint()
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

            var end = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 1f), cameraProjection, cameraView,
                new Vector2(Game.Width, Game.Height));
            var start = Vector3Ex.UnProject(new Vector3(Game.Mouse.X, Game.Mouse.Y, 0), cameraProjection, cameraView,
                new Vector2(Game.Width, Game.Height));
            var dir = (end - start).Normalized();
            var tgt = start + (dir * 400);

            if (world.Physics!.PointRaycast(player.PhysicsComponent!.Body, start, dir, 1000, out var contactPoint,
                    out _, out _))
            {
                return contactPoint;
            }

            return tgt;
        }

        private void ProcessInput(double delta)
        {
            if (Dead)
            {
                current_cur = cur_arrow;
                return;
            }

            if (paused)
            {
                return;
            }

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
                    steering.EngineKill = false;
                }

                else if (Input.IsActionDown(InputAction.USER_DEC_THROTTLE))
                {
                    shipInput.Throttle -= (float)(delta);
                    shipInput.Throttle = MathHelper.Clamp(shipInput.Throttle, 0, 1);
                    steering.EngineKill = false;
                }

                steering.Thrust = Input.IsActionDown(InputAction.USER_AFTERBURN);
                shipInput.Reverse = Input.IsActionDown(InputAction.USER_MANEUVER_BRAKE_REVERSE);
            }

            StrafeControls strafe = StrafeControls.None;

            if (!ui.KeyboardGrabbed)
            {
                if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_LEFT))
                {
                    strafe |= StrafeControls.Left;
                }

                if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_RIGHT))
                {
                    strafe |= StrafeControls.Right;
                }

                if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_UP))
                {
                    strafe |= StrafeControls.Up;
                }

                if (Input.IsActionDown(InputAction.USER_MANEUVER_SLIDE_EVADE_DOWN))
                {
                    strafe |= StrafeControls.Down;
                }
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
                        2f * (mX / (float)Game.Width) - 1f, -(2f * (mY / (float)Game.Height) - 1f)
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

            steering.CurrentStrafe = strafe;

            var obj = GetMouseSelection();

            if (ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y))
            {
                current_cur = cur_arrow;
            }
            else
            {
                current_cur = obj == null ? cur_cross : cur_reticle;
            }

            weapons.AimPoint = GetAimPoint();

            if (Input.IsActionDown(InputAction.USER_FIRE_WEAPONS))
            {
                weapons.FireAll(world);
            }

            if (Input.IsActionDown(InputAction.USER_LAUNCH_MISSILES))
            {
                weapons.FireMissiles(world);
            }

            for (int i = 0; i < 10; i++)
            {
                if (Input.IsActionDown(InputAction.USER_FIRE_WEAPON1 + i))
                {
                    weapons.FireIndex(i, world);
                }
            }

            if (world.Projectiles.HasMissilesQueued)
            {
                session.SpaceRpc.FireMissiles(world.Projectiles.GetMissileQueue());
            }
        }

        public void ClearComm()
        {
            ui.Event("Comm", [null]);
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
            if (!obj.TryGetComponent<CostumeComponent>(out var costume))
            {
                ClearComm();
                return;
            }

            if (!Game.GameData.Items.Ini.Voices.Voices.TryGetValue(voice, out var voiceData))
            {
                ClearComm();
                return;
            }

            var scripts = new List<AnmScript>();
            var canim = Game.GameData.GetCharacterAnimations();

            foreach (var s in voiceData.Scripts)
            {
                if (canim.Scripts.TryGetValue(s, out var sc))
                {
                    scripts.Add(sc);
                }
            }

            Accessory? acc = costume.Helmet;
            RigidModel? accModel = null;

            if (acc != null)
            {
                accModel = (costume.Helmet?.ModelFile!.LoadFile(Game.ResourceManager)!.Drawable as IRigidModelFile)
                    ?.CreateRigidModel(true, Game.ResourceManager);
            }

            var app = new CommAppearance()
            {
                Head = costume.Head?.LoadModel(Game.ResourceManager)!,
                Body = costume.Body?.LoadModel(Game.ResourceManager)!,
                Accessory = acc,
                AccessoryModel = accModel,
                Male = costume.Body?.Sex != FLGender.female,
                Scripts = scripts
            };

            string? factionName = null;

            if (obj.TryGetComponent<CFactionComponent>(out var fac) && fac?.Faction != null)
            {
                factionName = Game.GameData.GetString(fac.Faction.IdsName);
            }

            ui.Event("Comm", new CommData()
            {
                Source = obj.Name!.GetName(Game.GameData, Vector3.Zero)!,
                Affiliation = factionName,
                Appearance = app
            });
        }

        public void StartTradelane()
        {
            player.GetComponent<ShipPhysicsComponent>()!.Active = false;
            player.GetComponent<WeaponControlComponent>()!.Enabled = false;
            pilotComponent?.Cancel();
            RefreshActiveUserWaypoint(false);
        }

        public void TradelaneDisrupted()
        {
            Game.Sound.PlayVoiceLine(VoiceLines.NnVoiceName, VoiceLines.NnVoice.TradeLaneDisrupted);
            EndTradelane();
        }

        public void EndTradelane()
        {
            player.GetComponent<ShipPhysicsComponent>()!.Active = true;
            player.GetComponent<WeaponControlComponent>()!.Enabled = true;
        }

        private void GetCameraMatrices(out Matrix4x4 view, out Matrix4x4 projection)
        {
            view = activeCamera.View;
            projection = activeCamera.Projection;
        }

        private void GetViewProjection(out Matrix4x4 vp)
        {
            vp = activeCamera.ViewProjection;
        }

        private (Vector2 pos, bool visible) ScreenPosition(Vector3 worldPos)
        {
            GetViewProjection(out var vp);
            var clipSpace = Vector4.Transform(new Vector4(worldPos, 1), vp);
            var ndc = clipSpace / clipSpace.W;
            var windowSpace = new Vector2(
                ((ndc.X + 1.0f) / 2.0f) * Game.Width,
                ((1.0f - ndc.Y) / 2.0f) * Game.Height
            );

            var visible =
                windowSpace.X >= 0 &&
                windowSpace.X <= Game.Width &&
                windowSpace.Y >= 0 &&
                windowSpace.Y <= Game.Height;

            if (clipSpace.Z < 0)
            {
                windowSpace *= -1;
            }

            return (windowSpace, visible && ndc.Z < 1);
        }

        private (Vector2 pos, bool visible) ScreenPosition(GameObject obj)
        {
            return ScreenPosition(obj.WorldTransform.Position);
        }

        private GameObject? GetMouseSelection()
        {
            return GetWaypointScreenSelection(Game.Mouse.X, Game.Mouse.Y) ??
                   world.GetSelection(activeCamera, player, Game.Mouse.X, Game.Mouse.Y, Game.Width, Game.Height);
        }

        private GameObject? GetWaypointScreenSelection(float mouseX, float mouseY)
        {
            GameObject? result = null;
            var bestDistance = float.MaxValue;
            var playerPosition = player.WorldTransform.Position;
            foreach (var obj in world.Objects)
            {
                if (obj.Kind != GameObjectKind.Waypoint)
                {
                    continue;
                }

                var (pos, visible) = ScreenPosition(obj);
                if (!visible)
                {
                    continue;
                }

                var distance = Vector3.Distance(playerPosition, obj.WorldTransform.Position);
                var pickRadius = MathHelper.Clamp(distance / 220f, 18f, 90f);
                var mouseDistance = Vector2.Distance(new Vector2(mouseX, mouseY), pos);
                if (mouseDistance <= pickRadius && mouseDistance < bestDistance)
                {
                    result = obj;
                    bestDistance = mouseDistance;
                }
            }

            return result;
        }

        private void RemoveUserWaypoint()
        {
            if (userWaypoint == null)
            {
                return;
            }

            if (Selection.Selected == userWaypoint)
            {
                Selection.Selected = null;
            }

            world.RemoveObject(userWaypoint);
            userWaypoint = null;
        }

        private void ClearUserWaypoints()
        {
            session.ClearUserWaypoints();
            RemoveUserWaypoint();
        }

        private void CreateUserWaypoint(StarSystem system, Vector3 pos)
        {
            session.AddUserWaypoint(system, pos);
            RefreshActiveUserWaypoint(false);
        }

        private bool ComputeBestPathToSelection(StarSystem system, Vector3 pos)
        {
            var cruiseSpeed = player.GetFirstChildComponent<CEngineComponent>()?.Engine.CruiseSpeed ?? 300f;
            if (!session.ComputeBestPathToSelection(sys, player.WorldTransform.Position, system, pos, cruiseSpeed))
                return false;
            RefreshActiveUserWaypoint(false, false);
            return true;
        }

        private void RefreshActiveUserWaypoint(bool continueGoto, bool selectWaypoint = true)
        {
            RemoveUserWaypoint();
            if (session.TryGetActiveUserWaypoint(sys.CRC, out var waypoint))
                ActivateUserWaypoint(waypoint.Position, continueGoto, selectWaypoint);
        }

        private void ActivateUserWaypoint(Vector3 pos, bool continueGoto, bool selectWaypoint)
        {
            var waypointArch = Game.GameData.Items.Archetypes.Get("waypoint")!;
            userWaypoint = new GameObject(waypointArch, null, Game.ResourceManager)
            {
                Nickname = $"user_waypoint_{userWaypointCounter++}",
                Name = new ObjectName(1090) // Waypoint
            };
            userWaypoint.SetLocalTransform(new Transform3D(pos, Quaternion.Identity));
            world.AddObject(userWaypoint);
            userWaypoint.Register(world);

            if (selectWaypoint)
                Selection.Selected = userWaypoint;
            if (continueGoto)
            {
                pilotComponent!.GotoObject(userWaypoint, GotoKind.Goto);
            }
        }

        private void UpdateUserWaypointRoute()
        {
            if (paused || Dead || userWaypoint == null)
            {
                return;
            }

            if (!session.TryGetActiveUserWaypoint(sys.CRC, out var activeWaypoint) ||
                (activeWaypoint.Kind != UserWaypointKind.ManualDestination &&
                 activeWaypoint.Kind != UserWaypointKind.TradelaneExit))
            {
                return;
            }

            var playerPosition = player.WorldTransform.Position;
            var waypointPosition = userWaypoint.WorldTransform.Position;
            if (Vector3.Distance(playerPosition, waypointPosition) > UserWaypointReachDistance)
            {
                return;
            }

            // found it interesting that it could
            // follow the next waypoint if the player is on goto mode, which could be convenient
            // so their ship doesnt stop at the waypoints. False for now since its not vanilla,
            // left it here because its interesting for testing.
            const bool continueGoto = false;
            session.RemoveActiveUserWaypoint();
            RefreshActiveUserWaypoint(continueGoto);
        }

        private void UpdateBestPathRoute(double delta)
        {
            if (paused || Dead || session.InTradelane || !session.BestPathActive)
                return;

            bestPathRecheckTimer += delta;
            if (bestPathRecheckTimer < 1.0)
                return;
            bestPathRecheckTimer = 0;

            var cruiseSpeed = player.GetFirstChildComponent<CEngineComponent>()?.Engine.CruiseSpeed ?? 300f;
            if (session.RecalculateBestPath(sys, player.WorldTransform.Position, cruiseSpeed))
                RefreshActiveUserWaypoint(false, false);
        }

        private void UpdateWaypointRenderStyle()
        {
            var playerPosition = player.WorldTransform.Position;
            foreach (var obj in world.Objects)
            {
                if (obj.Kind != GameObjectKind.Waypoint)
                {
                    continue;
                }

                var selected = obj == Selection.Selected;
                var distance = Vector3.Distance(playerPosition, obj.WorldTransform.Position);
                var scale = selected ? MathHelper.Clamp(distance / 5000f, 1.5f, 18f) : 1f;
                if (obj.RenderComponent is ModelRenderer renderer)
                {
                    renderer.RenderScale = scale;
                    renderer.NoFog = selected;
                    renderer.ColorOverride = new Color4(0.55f, 0f, 1f, 1f);
                    renderer.Spin = new Vector3(0f, 5f, 0f);
                }
            }
        }

        private void UpdateObjectiveObjects()
        {
            if (missionWaypoint != null)
            {
                if (Selection.Selected == missionWaypoint)
                {
                    Selection.Selected = null;
                }

                world.RemoveObject(missionWaypoint);
                missionWaypoint = null;
            }

            if (TryGetMissionWaypointPosition(out var pos))
            {
                if (pos != Vector3.Zero)
                {
                    var waypointArch = Game.GameData.Items.Archetypes.Get("waypoint")!;
                    missionWaypoint = new GameObject(waypointArch, null, Game.ResourceManager)
                    {
                        Name = new ObjectName(1091) // Mission Waypoint
                    };
                    missionWaypoint.SetLocalTransform(new Transform3D(pos, Quaternion.Identity));
                    world.AddObject(missionWaypoint);
                    missionWaypoint.Register(world);
                }
            }
        }

        private void UpdateObjectiveRoute()
        {
            var objective = session.CurrentObjective;
            if (objective.Kind != ObjectiveKind.NavMarker ||
                string.IsNullOrWhiteSpace(objective.System))
            {
                if (session.BestPathActive)
                    ClearUserWaypoints();
                return;
            }

            var destination = Game.GameData.Items.Systems.Get(objective.System);
            if (destination == null || player == null || sys == null)
            {
                if (session.BestPathActive)
                    ClearUserWaypoints();
                return;
            }

            var cruiseSpeed = player.GetFirstChildComponent<CEngineComponent>()?.Engine.CruiseSpeed ?? 300f;
            if (session.ComputeBestPathToSelection(
                    sys,
                    player.WorldTransform.Position,
                    destination,
                    objective.Position,
                    cruiseSpeed))
            {
                RefreshActiveUserWaypoint(false, false);
            }
        }

        private bool TryGetMissionWaypointPosition(out Vector3 position)
        {
            position = Vector3.Zero;
            var objective = session.CurrentObjective;
            if (objective.Kind != ObjectiveKind.Object && objective.Kind != ObjectiveKind.NavMarker)
                return false;

            if (objective.Kind == ObjectiveKind.Object && !string.IsNullOrWhiteSpace(objective.Object))
            {
                var currentObject = world.GetObject(objective.Object);
                if (currentObject != null)
                {
                    position = currentObject.WorldTransform.Position;
                    return true;
                }
            }

            var objectiveSystem = objective.System;
            if (string.IsNullOrWhiteSpace(objectiveSystem))
                return false;

            if (sys.Nickname.Equals(objectiveSystem, StringComparison.OrdinalIgnoreCase))
            {
                position = objective.Kind == ObjectiveKind.NavMarker
                    ? objective.Position
                    : (world.GetObject(objective.Object)?.WorldTransform ?? Transform3D.Identity).Position;
                return position != Vector3.Zero;
            }

            var targetSystem = Game.GameData.Items.Systems.Get(objectiveSystem);
            if (targetSystem == null ||
                !sys.ShortestPathsAny.TryGetValue(targetSystem, out var path) ||
                path.Count < 2)
                return false;

            var nextSystem = path[1].Nickname;
            var jumpObject = sys.Objects.FirstOrDefault(x =>
                x.Dock is { Kind: DockKinds.Jump } &&
                x.Dock.Target?.Equals(nextSystem, StringComparison.OrdinalIgnoreCase) == true);
            if (jumpObject == null)
                return false;

            position = world.GetObject(jumpObject.Nickname)?.WorldTransform.Position ?? jumpObject.Position;
            return position != Vector3.Zero;
        }

        private int CrosshairSize()
        {
            float size = 14;
            float ratio = (Game.Height / 480f);
            return (int)(size * ratio);
        }

        private bool showObjectList = false;

        // RigidBody debugDrawBody;
        private int waitObjectiveFrames = 120;

        public override unsafe void Draw(double delta)
        {
            RenderMaterial.VertexLighting = false;

            if (loading)
            {
                loader!.Draw(delta);
                return;
            }

            if (Thn is { Running: true })
            {
                // Viewport FOV calculations unaffected by letterboxing
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

            if (Selection.Selected != null)
            {
                targetWireframe.Model = Selection.Selected.Model!.RigidModel;
                var lookAt = Matrix4x4.CreateLookAt(player.LocalTransform.Position,
                    Vector3.Transform(Vector3.UnitZ * 4, player.LocalTransform.Matrix()), Vector3.UnitY);

                targetWireframe.Matrix = (lookAt * Selection.Selected.LocalTransform.Matrix()).ClearTranslation();
                targetWireframe.ChildModels.Clear();

                foreach (var child in Selection.Selected.Children)
                {
                    if (child.Model == null ||
                        !GameObject.IsCargoPodChild(child))
                    {
                        continue;
                    }

                    var childMatrix = child.LocalTransform.Matrix();
                    if (child.Attachment != null)
                    {
                        childMatrix *= child.Attachment.Transform.Matrix();
                    }

                    var healthPct = 1f;
                    if (child.TryGetComponent<CHealthComponent>(out var health) && health.MaxHealth > 0)
                    {
                        healthPct = MathHelper.Clamp(health.CurrentHealth / health.MaxHealth, 0, 1);
                    }

                    targetWireframe.ChildModels.Add(new TargetShipWireframe.ChildModel(
                        child.Model.RigidModel,
                        childMatrix * targetWireframe.Matrix,
                        healthPct));
                }
            }

            if (updateStartDelay > 0)
            {
                updateStartDelay--;

                if (updateStartDelay == 0)
                {
                    session.UpdateStart(this);
                }
            }

            if (waitObjectiveFrames > 0)
            {
                waitObjectiveFrames--;
            }

            if (objectiveObjectsDirty)
            {
                objectiveObjectsDirty = false;
                UpdateObjectiveObjects();
            }

            UpdateWaypointRenderStyle();
            world.RenderUpdate(delta);
            sysrender.DebugRenderer.StartFrame(Game.RenderContext);

            sysrender.Draw(Game.RenderContext.CurrentViewport.Width, Game.RenderContext.CurrentViewport.Height);

            sysrender.DebugRenderer.Render();

            if (GetCrosshair(out var crosshairScreen, out _))
            {
                var sz = CrosshairSize();
                var r0 = new Rectangle((int)(crosshairScreen.X - sz / 2), (int)crosshairScreen.Y, sz, 1);
                var r1 = new Rectangle((int)crosshairScreen.X, (int)crosshairScreen.Y - (sz / 2), 1, sz);
                var dl = Game.RenderContext.Renderer2D.CreateDrawList();
                dl.FillRectangle(r0, Color4.Red);
                dl.FillRectangle(r1, Color4.Red);
                dl.Render();
            }

            if (!IsSpecialCamera() && ShowHud)
            {
                ui.Visible = true;

                if (nextObjectiveUpdate != 0 && waitObjectiveFrames <= 0)
                {
                    ui.Event("ObjectiveUpdate", nextObjectiveUpdate);
                    nextObjectiveUpdate = 0;
                }
            }
            else
            {
                ui.Visible = false;
            }

            if (Thn is { Running: true })
            {
                Game.RenderContext.PopViewport();
            }

            ui.RenderWidget(delta);
            session.SetDebug(Game.Debug.Enabled);
            DrawDebugWindow(delta);

            if ((!IsSpecialCamera() && ShowHud) || Game.Debug.Enabled || ui.HasModal)
            {
                var dlist = Game.RenderContext.Renderer2D.CreateDrawList();
                current_cur.Draw(dlist, Game.Mouse, Game.TotalTime);
                dlist.Render();
            }

            DoFade(delta);
        }

        public override void Exiting()
        {
            session.OnExit();
        }
    }
}
