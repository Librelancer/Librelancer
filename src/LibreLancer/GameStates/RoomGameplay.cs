// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using LibreLancer.Client;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.World;
using LibreLancer.Utf.Dfm;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Graphics.Text;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Infocards;
using LibreLancer.Input;
using LibreLancer.Interface;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Thn;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer
{
	public class RoomGameplay : GameState
	{
		static readonly string[] TOP_IDS = { //IDS that goes up the top?
			"IDS_HOTSPOT_DECK",
			"IDS_HOTSPOT_EQUIPMENTDEALER_ROOM",
			"IDS_HOTSPOT_SHIPDEALER_ROOM",
			"IDS_HOTSPOT_COMMODITYTRADER_ROOM",
			"IDS_HOTSPOT_BAR",
			"IDS_HOTSPOT_EXIT",
			"IDS_HOTSPOT_PLANETSCAPE"
		};
        const string LAUNCH_ACTION = "$LAUNCH";
        private const string INVALID_ACTION = "$INVALID";
		Base currentBase;
        private StarSystem sys;
        BaseRoom currentRoom;
		Cutscene scene;
        private UiContext ui;

		CGameSession session;
		string baseId;
        string active;
		Cursor cursor;
        private Cursor talk_story;
		string virtualRoom;
        List<BaseHotspot> tophotspots;
        private ThnScript[] sceneScripts;
        private ThnScript waitingForFinish;
        private StoryCutsceneIni currentCutscene;
        private ScriptState currentState = ScriptState.None;
        private Infocard roomInfocard;
        enum ScriptState
        {
            None,
            Cutscene,
            Enter,
            Launch
        }

        private bool paused = false;

        private int nextObjectiveUpdate = 0;

        public RoomGameplay(FreelancerGame g, CGameSession session, string newBase, BaseRoom room = null, string virtualRoom = null) : base(g)
        {
            //Load room data
            this.session = session;
			baseId = newBase;
			currentBase = g.GameData.Items.Bases.Get(newBase);
			currentRoom = room ?? currentBase.StartRoom;
            currentRoom.InitForDisplay();
            var rm = virtualRoom ?? currentRoom.Nickname;
            this.virtualRoom = virtualRoom;
            //Find infocard
            sys = g.GameData.Items.Systems.Get(currentBase.System);
            var obj = sys.Objects.FirstOrDefault((o) => o.Base == currentBase);
            int ids = obj?.IdsInfo ?? 0;
            roomInfocard = g.GameData.GetInfocard(ids, g.Fonts);
            if (g.GameData.GetRelatedInfocard(ids, g.Fonts, out var ic2))
            {
                roomInfocard.Nodes.Add(new RichTextParagraphNode());
                roomInfocard.Nodes.AddRange(ic2.Nodes);
            }
            //Create user interface
            tophotspots = new List<BaseHotspot>();
            foreach (var hp in currentRoom.Hotspots)
                if (TOP_IDS.Contains(hp.Name))
                    tophotspots.Add(hp);
            SetActiveHotspot(rm);
            Game.Keyboard.TextInput += Game_TextInput;
            Game.Keyboard.KeyDown += Keyboard_KeyDown;
            Game.Mouse.MouseDown += MouseOnMouseDown;
            cursor = Game.ResourceManager.GetCursor("arrow");
            talk_story = Game.ResourceManager.GetCursor("talk_story");
            ui = Game.Ui;
            nextObjectiveUpdate = session.CurrentObjective.Ids;
            session.ObjectiveUpdated = () => nextObjectiveUpdate = session.CurrentObjective.Ids;
            ui.GameApi = new BaseUiApi(this);
            ui.OpenScene("baseside");
            //Set up THN
            SwitchToRoom(room == null && session.PlayerShip != null);
            FadeIn(0.8, 1.7);
            GC.Collect();
        }

        public override void OnSettingsChanged()
        {
            if (scene?.Renderer != null)
                scene.Renderer.Settings = Game.Config.Settings;
        }


        protected override void OnActionDown(InputAction action)
        {
            if (ui.KeyboardGrabbed) return;
            switch (action)
            {
                case InputAction.USER_CHAT:
                    ui.ChatboxEvent();
                    break;
            }
        }

        private void MouseOnMouseDown(MouseEventArgs e)
        {
            RTCHotspot hp;
            if ((hp = GetHotspot(e.X, e.Y)) != null) {
                PlayScript(hp.ini, CutsceneState.Regular);
                session.RpcServer.StoryNPCSelect(hp.npc, currentRoom.Nickname, currentBase.Nickname);
            }
        }

        void MissionAccepted()
        {
            session.RpcServer.RTCMissionAccepted();
            PlayScript(currentCutscene, CutsceneState.Accept);
        }

        void MissionRejected()
        {
            session.RpcServer.RTCMissionRejected();
            PlayScript(currentCutscene, CutsceneState.Reject);
        }

        void SetActiveHotspot(string rm)
        {
            foreach (var hp in tophotspots) {
                if (hp.SetVirtualRoom == rm) {
                    active = hp.Name;
                    return;
                }
            }
            foreach (var hp in tophotspots) {
                if (hp.Room == rm) {
                    active = hp.Name;
                    return;
                }
            }
        }

        [WattleScript.Interpreter.WattleScriptUserData]
        public class BaseUiApi : UiApi
        {
            RoomGameplay g;
            private NewsArticle[] articles;
            public BaseUiApi(RoomGameplay g)
            {
                this.g = g;
                articles = g.session.News;
                Trader = new Trader(g.session);
                ShipDealer = new ShipDealer(g.session);
            }

            public int CurrentRank => g.session.CurrentRank;
            public double NetWorth => (double)g.session.NetWorth;
            public double NextLevelWorth => (double)g.session.NextLevelWorth;
            public PlayerStats Statistics => g.session.Statistics;

            public double CharacterPlayTime => g.session.CharacterPlayTime;
            public bool HasShip() => g.session.PlayerShip != null;

            public GameSettings GetCurrentSettings() => g.Game.Config.Settings.MakeCopy();

            public int GetObjectiveStrid() => g.session.CurrentObjective.Ids;

            public DisplayFaction[] GetPlayerRelations() => g.session.GetUIRelations();

            public KeyMapTable GetKeyMap()
            {
                var table = new KeyMapTable(g.Game.InputMap, g.Game.GameData.Items.Ini.Infocards);
                table.OnCaptureInput += (k) =>
                {
                    g.Input.KeyCapture = k;
                };
                return table;
            }

            public void ApplySettings(GameSettings settings)
            {
                g.Game.Config.Settings = settings;
                g.Game.Config.Save();
            }

            public void MissionResponse(string r)
            {
                if(r == "accept")
                    g.MissionAccepted();
                else
                    g.MissionRejected();
            }

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

            public void PopupFinish(string id)
            {
                g.waitObjectiveFrames = 30;
                g.session.RpcServer.ClosedPopup(id);
            }

            public void SaveGame(string description)
            {
                g.session.Save(description);
            }

            public void Resume()
            {
                g.paused = false;
            }

            public void QuitToMenu()
            {
                g.session.QuitToMenu();
            }

            public NewsArticle[] GetNewsArticles() => articles;
            public bool IsMultiplayer() => g.session.Multiplayer;
            public void HotspotPressed(string item) => g.Hud_OnManeuverSelected(item);
            public string ActiveNavbarButton() => g.active;

            public Infocard CurrentInfocard() => g.roomInfocard;

            public string CurrentInfoString() => null;

            public double GetCredits() => g.session.Credits;

            public Trader Trader;
            public ShipDealer ShipDealer;

            public ChatSource GetChats() => g.session.Chats;

            public void PopulateNavmap(Navmap navmap)
            {
                navmap.PopulateIcons(g.ui, g.sys);
                navmap.SetVisitFunction(g.session.IsVisited);
            }

            bool IsVisited(uint hash)
            {
                if (!g.session.Visits.TryGetValue(hash, out var visit))
                    return false;
                return (visit & VisitFlags.Hidden) != VisitFlags.Hidden &&
                       (visit & VisitFlags.Visited) == VisitFlags.Visited;
            }

            public NavbarButtonInfo[] GetNavbarButtons()
            {
                var buttons = new NavbarButtonInfo[g.tophotspots.Count];
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i] = new NavbarButtonInfo(
                        g.tophotspots[i].Name,
                        g.tophotspots[i].SetVirtualRoom ?? g.tophotspots[i].Room
                    );
                }

                return buttons;
            }
            public NavbarButtonInfo[] GetActionButtons()
            {
                var actions = new List<NavbarButtonInfo>();
                string cRoom = (string.IsNullOrEmpty(g.virtualRoom) ? g.currentRoom.Nickname : g.virtualRoom)
                    .ToLowerInvariant();
                switch (cRoom)
                {
                    case "cityscape":
                    case "deck":
                    case "planetscape":
                        if(g.session.PlayerShip != null)
                            actions.Add(new NavbarButtonInfo(LAUNCH_ACTION, "IDS_HOTSPOT_LAUNCH"));
                        break;
                    case "bar":
                        if(g.session.News?.Length > 0)
                            actions.Add(new NavbarButtonInfo(INVALID_ACTION, "IDS_HOTSPOT_NEWSVENDOR"));
                        break;
                }

                foreach (var hp in g.currentRoom.Hotspots)
                {
                    if (!string.IsNullOrEmpty(hp.VirtualRoom) &&
                        !hp.VirtualRoom.Equals(g.virtualRoom, StringComparison.OrdinalIgnoreCase))
                        continue;
                    switch (hp.Name.ToUpperInvariant())
                    {
                        case "IDS_HOTSPOT_COMMODITYTRADER":
                            actions.Add(new NavbarButtonInfo("CommodityTrader", hp.Name));
                            break;
                        case "IDS_HOTSPOT_SHIPDEALER":
                            actions.Add(new NavbarButtonInfo("ShipDealer", hp.Name));
                            break;
                        case "IDS_HOTSPOT_EQUIPMENTDEALER":
                            actions.Add(new NavbarButtonInfo("EquipmentDealer", hp.Name));
                            break;
                    }
                }
                return actions.ToArray();
            }
            public void ChatEntered(ChatCategory category, string text)
            {
                g.session.OnChat(category, text);
            }
        }
		protected override void OnUnload()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
            Game.Mouse.MouseDown -= MouseOnMouseDown;
			scene.Dispose();
		}

        private HashSet<string> processedPaths = new HashSet<string>();
        private Queue<StoryCutsceneIni> toPlay = new Queue<StoryCutsceneIni>();
        bool ProcessCutscenes()
        {
            foreach (var ct in session.ActiveCutscenes)
            {
                if (processedPaths.Contains(ct.RefPath.ToLowerInvariant())) continue;
                if (ct.Encounters.Count != 1) continue;
                var n = ct.Encounters[0].Location;
                //Force player to new room
                if (ct.Encounters[0].StartRoom != null &&
                    n[0].Equals(currentBase.Nickname, StringComparison.OrdinalIgnoreCase) &&
                    !ct.Encounters[0].StartRoom.Equals(currentRoom.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    var rm = currentBase.Rooms.Get(ct.Encounters[0].StartRoom);
                    Game.ChangeState(new RoomGameplay(Game, session, baseId, rm));
                    return true;
                }
                else if (n[0].Equals(currentBase.Nickname, StringComparison.OrdinalIgnoreCase) &&
                         n[1].Equals(currentRoom.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    processedPaths.Add(ct.RefPath.ToLowerInvariant());
                    if (ct.Encounters[0].Autoplay) {
                        PlayScript(ct, CutsceneState.Regular);
                    } else {
                        toPlay.Enqueue(ct);
                    }
                }
            }
            if (currentCutscene == null && toPlay.Count > 0)
            {
                ProcessNextCutscene();
            }
            return false;
        }

        enum CutsceneState
        {
            None,
            Offer,
            Decision,
            Accept,
            Reject,
            Regular
        }

        private CutsceneState cState = CutsceneState.None;
        void PlayScript(StoryCutsceneIni ct, CutsceneState state)
        {
            ui.Visible = false;
            cState = CutsceneState.Regular;
            string scName = ct.Encounters[0].Action;
            if (!string.IsNullOrEmpty(ct.Encounters[0].Offer)) {
                scName = ct.Encounters[0].Offer;
                cState = CutsceneState.Offer;
            }
            switch (state)
            {
                case CutsceneState.Decision:
                    scName = ct.Encounters[0].Decision;
                    cState = state;
                    break;
                case CutsceneState.Accept:
                    scName = ct.Encounters[0].Accept;
                    cState = state;
                    break;
                case CutsceneState.Reject:
                    scName = ct.Encounters[0].Reject;
                    cState = state;
                    break;
            }
            var script = new ThnScript(session.Game.GameData.VFS.ReadAllBytes(session.Game.GameData.Items.DataPath(scName)), session.Game.GameData.Items.ThornReadCallback, scName);
            currentCutscene = ct;
            RoomDoSceneScript(script, ScriptState.Cutscene);
        }



        private bool didLaunch = false;
        public void Launch()
        {
            if (!string.IsNullOrEmpty(currentRoom.LaunchScript?.DataPath))
            {
                RoomDoSceneScript(currentRoom.LaunchScript.LoadScript(), ScriptState.Launch);
            }
            else
            {
                SendLaunch();
            }
        }

        void SendLaunch()
        {
            if (didLaunch) return;
            session.Launch();
            didLaunch = true;
        }

        void Hud_OnManeuverSelected(string arg)
        {
            if (arg == active) return;
            if (arg == INVALID_ACTION) return;
            Game.QueueUIThread(() => //Fixes stack trace
            {
                if(arg == LAUNCH_ACTION)
                {
                    Launch();
                    return;
                }

                var hotspot = currentRoom.Hotspots.Find((obj) => obj.Name == arg);
                switch (hotspot.Behavior)
                {
                    case "ExitDoor":
                        var rm = currentBase.Rooms.Get(hotspot.Room);
                        FadeOut(0.6, () => Game.ChangeState(new RoomGameplay(Game, session, baseId, rm, hotspot.SetVirtualRoom)));
                        break;
                    case "VirtualRoom":
                        FadeOut(0.6, () => Game.ChangeState(new RoomGameplay(Game, session, baseId, currentRoom, hotspot.Room)));
                        break;
                }
            });
		}

		void Keyboard_KeyDown(KeyEventArgs e)
        {
            if (KeyCaptureContext.Capturing(Input.KeyCapture)) return;
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
                if (e.Key == Keys.Escape && !paused)
                {
                    switch (currentState)
                    {
                        case ScriptState.Launch:
                            SendLaunch();
                            break;
                        case ScriptState.Cutscene:
                        case ScriptState.Enter:
                            SceneOnScriptFinished(waitingForFinish);
                            break;
                    }
                }
                if (e.Key == Keys.F1 && !paused)
                {
                    paused = true;
                    ui.Event("Pause");
                }
			}
		}

		void Game_TextInput(string text)
		{
            ui.OnTextEntry(text);
		}

        private GameObject playerShip;

        void CreatePlayerEquipment()
        {
            if (playerShip.RenderComponent != null)
            {
                playerShip.Children.Clear();
                foreach (var mount in session.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                {
                    EquipmentObjectManager.InstantiateEquipment(playerShip, Game.ResourceManager, null,
                            EquipmentType.Cutscene, mount.Hardpoint, mount.Equipment);
                }
            }
        }

        IEnumerable<ThnScript> GetMsnAmbients()
        {
            var r = virtualRoom ?? currentRoom.Nickname;
            var b = currentBase.Nickname;
            foreach (var x in session.Thns.Ambients.Where(x =>
                         x.Base.Equals(b, StringComparison.OrdinalIgnoreCase) &&
                         x.Room.Equals(r, StringComparison.OrdinalIgnoreCase)))
            {
                ThnScript script = null;
                try
                {
                    script = new ThnScript(session.Game.GameData.VFS.ReadAllBytes(session.Game.GameData.Items.DataPath(x.Script)), session.Game.GameData.Items.ThornReadCallback, x.Script);
                }
                catch (Exception e)
                {
                    FLLog.Error("Thn", $"Error loading Ambient script: {e}");
                }
                if (script != null)
                    yield return script;
            }
        }

		void SwitchToRoom(bool dolanding)
        {
            Game.Saves.Selected = -1;
            session.RoomEntered(virtualRoom ?? currentRoom.Nickname, currentBase.Nickname);
			if (currentRoom.Music == null)
			{
				Game.Sound.StopMusic();
			}
			else
			{
				Game.Sound.PlayMusic(currentRoom.Music, 0, currentRoom.MusicOneShot);
			}

            if (session.PlayerShip != null)
            {
                playerShip = new GameObject(session.PlayerShip.ModelFile.LoadFile(Game.ResourceManager), Game.ResourceManager, true,
                    false);
                CreatePlayerEquipment();
            }
            else
            {
                playerShip = new GameObject(); //Empty
            }

            session.OnUpdatePlayerShip = CreatePlayerEquipment;
            var ctx = new ThnScriptContext(currentRoom.OpenSet());
            ctx.PlayerShip = playerShip;
            if (playerShip.TryGetComponent<CEngineComponent>(out var cengine))
            {
                ctx.PlayerEngine = cengine;
                cengine.Active = false;
                cengine.PlaySound = false;
            }
            if(currentBase.TerrainTiny != null) ctx.Substitutions.Add("$terrain_tiny", currentBase.TerrainTiny);
            if(currentBase.TerrainSml != null) ctx.Substitutions.Add("$terrain_sml", currentBase.TerrainSml);
            if(currentBase.TerrainMdm != null) ctx.Substitutions.Add("$terrain_mdm", currentBase.TerrainMdm);
            if(currentBase.TerrainLrg != null) ctx.Substitutions.Add("$terrain_lrg", currentBase.TerrainLrg);
            if(currentBase.TerrainDyna1 != null) ctx.Substitutions.Add("$terrain_dyna_01", currentBase.TerrainDyna1);
            if(currentBase.TerrainDyna2 != null) ctx.Substitutions.Add("$terrain_dyna_02", currentBase.TerrainDyna2);
            scene = new Cutscene(ctx, Game.GameData, Game.ResourceManager, Game.Sound, Game.RenderContext.CurrentViewport, Game);
            scene.ScriptFinished += SceneOnScriptFinished;

            sceneScripts = currentRoom.OpenScene().Concat(GetMsnAmbients()).ToArray();

            if (dolanding && !string.IsNullOrEmpty(currentRoom.LandScript?.DataPath))
            {
                RoomDoSceneScript(currentRoom.LandScript.LoadScript(), ScriptState.Enter);
            } else if (!string.IsNullOrEmpty(currentRoom.StartScript?.DataPath))
            {
                RoomDoSceneScript(currentRoom.StartScript.LoadScript(), ScriptState.Enter);
            }
            else
            {
                RoomDoSceneScript(null, ScriptState.None);
            }
        }

        Vector2 ScreenPosition(Vector3 worldPos)
        {
            var clipSpace = Vector4.Transform(new Vector4(worldPos, 1), scene.CameraHandle.ViewProjection);
            var ndc = clipSpace / clipSpace.W;
            var viewSize = new Vector2(Game.Width, Game.Height);
            var windowSpace = new Vector2(
                ((ndc.X + 1.0f) / 2.0f) * Game.Width,
                ((1.0f - ndc.Y) / 2.0f) * Game.Height
            );
            return windowSpace;
        }

        class RTCHotspot
        {
            public ThnObject obj;
            public StoryCutsceneIni ini;
            public string npc;
        }
        private List<RTCHotspot> hotspots = new List<RTCHotspot>();

        RTCHotspot GetHotspot(int mX, int mY)
        {
            foreach (var hp in hotspots)
            {
                var sp = ScreenPosition(hp.obj.Translate);
                var rect = new RectangleF(
                    sp.X - 50, sp.Y - 50, 100, 100);
                if (rect.Contains(mX, mY)) return hp;
            }

            return null;
        }



        void ProcessNextCutscene()
        {
            var ct = toPlay.Dequeue();
            int position = 0;
            int i = 0;
            foreach(var npc in ct.Chars)
            {
                var obj = new GameObject() {Nickname = npc.Actor};
                var costumeName = Game.GameData.GetCostumeForNPC(npc.Npc);
                Game.GameData.GetCostume(costumeName, out var body, out var head, out var lh, out var rh);
                var skel = new DfmSkeletonManager(
                    body?.LoadModel(Game.ResourceManager),
                    head?.LoadModel(Game.ResourceManager),
                    lh?.LoadModel(Game.ResourceManager),
                    rh?.LoadModel(Game.ResourceManager));
                obj.RenderComponent = new CharacterRenderer(skel);
                var anmComponent = new AnimationComponent(obj, Game.GameData.GetCharacterAnimations());
                obj.AnimationComponent = anmComponent;
                obj.AddComponent(anmComponent);
                string spot = npc.Spot;
                if (string.IsNullOrEmpty(spot)) {
                    spot = ct.Reserves[0].Spot[position++];
                }
                var pos = scene.GetObject(spot).Translate;
                obj.SetLocalTransform(new Transform3D(pos, Quaternion.Identity));
                var thnObj = new ThnObject();
                thnObj.Name = npc.Actor;
                thnObj.Rotate = Quaternion.Identity;
                thnObj.Translate = pos;
                thnObj.Object = obj;
                scene.AddObject(thnObj);
                scene.FidgetScript(new ThnScript(
                    session.Game.GameData.VFS.ReadAllBytes(session.Game.GameData.Items.DataPath(npc.Fidget)),
                    session.Game.GameData.Items.ThornReadCallback, npc.Fidget));
                if(i == 0) hotspots.Add(new RTCHotspot() { ini = ct, obj = thnObj, npc = npc.Npc });
                i++;
            }
        }

        private void SceneOnScriptFinished(ThnScript obj)
        {
            waitObjectiveFrames = 50;
            if (waitingForFinish != null && obj == waitingForFinish)
            {
                if (currentCutscene != null)
                {
                    if (cState == CutsceneState.Decision)
                    {
                        return;
                    }
                    FadeOut(0.25, () =>
                    {
                        RoomDoSceneScript(null, ScriptState.None);
                        ui.Visible = true;
                        FLLog.Info("Thn", "Finished cutscene");
                        if (cState == CutsceneState.Regular ||
                            cState == CutsceneState.Accept ||
                            cState == CutsceneState.Reject)
                        {
                            session.FinishCutscene(currentCutscene);
                            if (currentCutscene.Encounters[0].RelocatePlayer != null) {
                                var rm = currentBase.Rooms.Get(currentCutscene.Encounters[0].RelocatePlayer);
                                Game.ChangeState(new RoomGameplay(Game, session, baseId, rm));
                            }
                            else
                            {
                                currentCutscene = null;
                                if (toPlay.Count > 0)
                                {
                                    ProcessNextCutscene();
                                }
                            }
                        }
                        else if (cState == CutsceneState.Offer)
                        {
                            PlayScript(currentCutscene, CutsceneState.Decision);
                            ui.Event("MissionOffer", currentCutscene.Encounters[0].MissionTextId);
                        }
                        FadeIn(0.2,0.25);
                    });
                }
                else if (currentState == ScriptState.Launch)
                {
                    SendLaunch();
                }
                else
                {
                    currentState = ScriptState.None;
                    SetRoomCameraAndShip();
                    animatingLetterbox = true;
                }
            }
        }

        private double letterboxAmount = 1;
        private bool animatingLetterbox = false;

        IEnumerable<ThnScript> Scripts(params IEnumerable<ThnScript>[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    foreach (var scn in obj)
                        if (scn != null)
                            yield return scn;
                }
            }
        }

        void SetRoomCameraAndShip()
        {
            if (currentRoom.Camera != null)
                scene.SetCamera(currentRoom.Camera);
            ThnObject shipMarker = scene.GetObject(currentRoom.PlayerShipPlacement);
            if (shipMarker != null) {
                if(playerShip.HardpointExists("HpMount"))
                {
                    playerShip.SetLocalTransform(playerShip.GetHardpoint("HpMount").Transform.Inverse());
                }
                else
                {
                    playerShip.SetLocalTransform(Transform3D.Identity);
                }
                shipMarker.Object.Children.Add(playerShip);
            }
        }

        private bool firstFrame = false; //Stops a desync in scene starting
        void RoomDoSceneScript(ThnScript sc, ScriptState state)
        {
            hotspots = new List<RTCHotspot>();
            firstFrame = true;
            currentState = state;
            if (sc == null) currentState = ScriptState.None;
            waitingForFinish = sc;
            scene.BeginScene(Scripts(sceneScripts, new[] { sc }));
            string[] ships = Array.Empty<string>();
            if (session.Ships != null) {
                ships = session.Ships.Select(x => Game.GameData.Items.Ships.Get(x.ShipCRC).Nickname).ToArray();
            }
            for(int i = 0; (i < ships.Length && i < currentRoom.ForSaleShipPlacements.Count); i++)
            {
                ThnObject marker = scene.GetObject(currentRoom.ForSaleShipPlacements[i]);
                if(marker == null)
                {
                    FLLog.Error("Base", "Couldn't display " + ships[i] + " on " + currentRoom.ForSaleShipPlacements[i]);
                    continue;
                }
                var toSellShip = Game.GameData.Items.Ships.Get(ships[i]);
                //Set up object
                var obj = new GameObject(toSellShip.ModelFile.LoadFile(Game.ResourceManager), Game.ResourceManager, true, false) { Parent = marker.Object };
                marker.Object.Children.Add(obj);
                if(obj.HardpointExists("HpMount"))
                {
                    obj.SetLocalTransform(obj.GetHardpoint("HpMount").Transform.Inverse());
                }
            }
            if (sc == null) {
                SetRoomCameraAndShip();
                letterboxAmount = -1;
                ui.Visible = true;
            }
            else {
                ui.Visible = false;
                letterboxAmount = 1;
            }
            if (cState == CutsceneState.Decision)
                letterboxAmount = -1;
        }

        private const int OBJECTIVE_WAIT_TIME = 16;
        private int waitObjectiveFrames = 16;
		public override void Update(double delta)
        {
            waitObjectiveFrames--;
            if (waitObjectiveFrames < 0) waitObjectiveFrames = 0;
            session.Update();
            if (ProcessCutscenes())
                return;
            if (scene != null) {
                if(paused)
                    scene.Update(0);
                else
                    scene.Update(firstFrame ? 0 : delta);
            }
            firstFrame = false;
            if (!firstFrame) {
                if (session.Popups.Count > 0 && session.Popups.TryDequeue(out var popup))
                {
                    FLLog.Debug("Room", "Displaying popup");
                    ui.Event("Popup", popup.Title, popup.Contents, popup.ID);
                }
            }
            ui.Update(Game);
            Game.TextInputEnabled = ui.KeyboardGrabbed;
        }



        public override void Draw(double delta)
        {
            RenderMaterial.VertexLighting = true;
            if (scene != null)
            {
                if(letterboxAmount > 0)
                {
                    Game.RenderContext.ClearColor = Color4.Black;
                    Game.RenderContext.ClearColorOnly();
                    var newRatio = ((double)Game.Width / Game.Height) * 1.39;
                    var newHeight = Game.Width / newRatio;
                    var diff = (Game.Height - newHeight);
                    var vp = Game.RenderContext.CurrentViewport;
                    vp.Y = (int)((diff / 2) * letterboxAmount);
                    vp.Height = (int)(Game.Height - (diff) * letterboxAmount);
                    Game.RenderContext.PushViewport(vp.X, vp.Y, vp.Width, vp.Height);
                    Game.RenderContext.PushScissor(new Rectangle(vp.X, vp.Y, vp.Width, vp.Height), false);
                }
                scene.UpdateViewport(Game.RenderContext.CurrentViewport, (float)Game.Width / Game.Height);
                scene.Draw(delta, Game.RenderContext.CurrentViewport.Width, Game.RenderContext.CurrentViewport.Height);
                if (letterboxAmount > 0)
                {
                    Game.RenderContext.PopScissor();
                    Game.RenderContext.PopViewport();
                }
            }

            ui.RenderWidget(delta);
            DoFade(delta);
            if (animatingLetterbox)
            {
                letterboxAmount -= delta * 4.5;
                if (letterboxAmount < 0)
                {
                    letterboxAmount = -1;
                    animatingLetterbox = false;
                    ui.Visible = true;
                }
            }
            Game.Debug.Draw(delta, () =>
            {
                ImGui.Text($"Room: {currentRoom.Nickname}");
                ImGui.Text($"Virtual: {virtualRoom ?? "NONE"}");
            }, () =>
            {
                Game.Debug.MissionWindow(session.GetTriggerInfo());
                Game.Debug.ObjectsWindow(scene.World.Objects);
            });

            if (ui.Visible && !ui.HasModal && nextObjectiveUpdate != 0 && waitObjectiveFrames <= 0)
            {
                ui.Event("ObjectiveUpdate", nextObjectiveUpdate);
                nextObjectiveUpdate = 0;
            }
            if (ui.Visible || ui.HasModal || Game.Debug.Enabled)
            {
                if(GetHotspot(Game.Mouse.X, Game.Mouse.Y) != null) talk_story.Draw(Game.RenderContext.Renderer2D, Game.Mouse, Game.TotalTime);
                else cursor.Draw(Game.RenderContext.Renderer2D, Game.Mouse, Game.TotalTime);
            }
        }

        public override void Exiting()
        {
            session.OnExit();
        }
    }
}
