using System;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Client;
using LibreLancer.Client.Components;
using LibreLancer.Graphics;
using LibreLancer.Infocards;
using LibreLancer.Interface;
using LibreLancer.Net;
using LibreLancer.World;
using LibreLancer.World.Components;
using WattleScript.Interpreter;

namespace LibreLancer;

partial class SpaceGameplay
{
    public bool ShowHud = true;
    private UiContext ui;
    private LuaAPI uiApi;
    private UiRenderable? steerArrow;


    private void CreateHud()
    {
        ui = Game.Ui;
        ui.GameApi = uiApi = new LuaAPI(this);
        uiApi.IndicatorLayer.OnRender += IndicatorLayerOnRender;
    }

    private UiRenderable SteerArrow()
    {
        return steerArrow ??= [
            new DisplayModel(new InterfaceModel { Path = @"interface\hud\hud_steeringarrow.3db" })
            {
                BaseRadius = 1f
            }
        ];
    }

    private (Vector2, float) ArrowPosition(Vector2 pos)
    {
        var screenCenter = new Vector2(ui.ScreenWidth, 480) / 2f;
        pos -= screenCenter;

        var angle = -(MathF.PI / 2) - MathF.Atan2(pos.Y, -pos.X);

        var cos = MathF.Cos(angle);
        var sin = -MathF.Sin(angle);
        var m = cos / sin;
        var screenBounds = screenCenter * 0.9f;

        if (cos > 0)
        {
            pos = screenBounds with
            {
                X = screenBounds.Y / m
            };
        }
        else
        {
            pos = new Vector2(-screenBounds.Y / m, -screenBounds.Y);
        }

        if (pos.X > screenBounds.X)
        {
            pos = screenBounds with
            {
                Y = screenBounds.X * m
            };
        }
        else if (pos.X < -screenBounds.X)
        {
            pos = new Vector2(-screenBounds.X, -screenBounds.X * m);
        }

        pos = -pos;
        pos += screenCenter;

        return (pos, angle);
    }

    private void DrawSelectedArrow(double delta, GameObject obj, Vector2 pos, UiContext context, DrawList2D drawList,
        RectangleF parentRectangle)
    {
        var rep = GetRepToPlayer(obj) switch
        {
            RepAttitude.Friendly => "friendly",
            RepAttitude.Hostile => "hostile",
            _ => "neutral"
        };
        var (arrowPos, angle) = ArrowPosition(pos);
        uiApi.SelectedArrow?.Draw(
            context, delta, drawList, parentRectangle, arrowPos.X, arrowPos.Y,
            angle, rep, (obj.Flags & GameObjectFlags.Important) != 0
        );
    }

    private void DrawUnselectedArrow(double delta, GameObject obj, Vector2 pos, UiContext context, DrawList2D drawList,
        RectangleF parentRectangle)
    {
        var rep = GetRepToPlayer(obj) switch
        {
            RepAttitude.Friendly => "friendly",
            RepAttitude.Hostile => "hostile",
            _ => "neutral"
        };
        var (arrowPos, angle) = ArrowPosition(pos);
        uiApi.UnselectedArrow?.Draw(
            context, delta, drawList, parentRectangle, arrowPos.X, arrowPos.Y,
            angle, rep, 0.5f, (obj.Flags & GameObjectFlags.Important) != 0
        );
    }

    private void DrawShipReticle(double delta, GameObject obj, Vector2 pos, UiContext context,
        RectangleF parentRectangle)
    {
        // var rep = GetRepToPlayer(obj);
    }

    private void DrawSteeringArrows(UiContext context, DrawList2D drawList)
    {
        var steeringActive = ((isLeftDown && leftDownTimer < 0) || mouseFlight) &&
                             control.Active &&
                             !ui.MouseWanted(Game.Mouse.X, Game.Mouse.Y);
        if (!steeringActive || isTurretView)
        {
            return;
        }

        var steer = Game.GameData.Items.Ini.Hud.Steer;
        if (steer.Radius <= 0 || steer.Range <= 0 || steer.Size <= 0)
        {
            return;
        }

        var mouse = ui.PixelsToPoints(new Vector2(Game.Mouse.X, Game.Mouse.Y));
        var center = new Vector2(ui.ScreenWidth, 480) / 2f;
        var offset = mouse - center;
        var distance = offset.Length();
        if (distance < steer.Radius)
        {
            return;
        }

        var direction = offset / distance;
        var maxCount = Math.Max(1, (int)MathF.Floor(steer.Range / steer.Radius));
        var count = Math.Min(maxCount, (int)MathF.Floor(distance / steer.Radius));
        var angle = MathF.Atan2(direction.Y, direction.X) + (MathF.PI / 2f);
        var arrow = SteerArrow();
        var textColor = context.Data.GetColor("text").GetColor(context.GlobalTime);

        if (arrow.GetElement(0) is DisplayModel model)
        {
            model.Rotate = new Vector3(0, 0, -angle);
        }

        for (int i = 1; i <= count; i++)
        {
            var t = maxCount <= 1 ? 1f : (i - 1) / (float)(maxCount - 1);
            var size = MathHelper.Lerp(steer.Size * 1.1f, steer.Size * 3.0f, t);
            var alpha = MathHelper.Lerp(0.55f, 1f, t);
            var color = textColor;
            color.A *= alpha;
            var point = center + (direction * (steer.Radius * i));
            var rect = new RectangleF(
                point.X - (size / 2f),
                point.Y - (size / 2f),
                size,
                size
            );
            arrow.Draw(context, drawList, rect, color);
        }
    }

    private void DrawWaypoint(double delta, GameObject obj, Vector2 pos, UiContext context, DrawList2D drawList,
        RectangleF parentRectangle, bool selected)
    {
        var size = WaypointSelectionStartSize;
        if (selected)
        {
            if (selectedWaypointAnimationObject != obj.Unique)
            {
                selectedWaypointAnimationObject = obj.Unique;
                selectedWaypointAnimationStart = Game.TotalTime;
            }

            var t = MathHelper.Clamp(
                (float)((Game.TotalTime - selectedWaypointAnimationStart) / WaypointSelectionAnimationDuration),
                0f,
                1f
            );
            size = MathHelper.Lerp(WaypointSelectionStartSize, WaypointSelectionEndSize, t);
        }

        var alpha = selected ? 1f : 0.85f;
        uiApi.Waypoint?.Draw(
            context, delta, drawList, parentRectangle,
            ui.PixelsToPoints(pos.X) - (size / 2f),
            ui.PixelsToPoints(pos.Y) - (size / 2f),
            size, alpha
        );
    }

    private void IndicatorLayerOnRender(UiContext context, double delta, DrawList2D drawList,
        RectangleF clientRectangle)
    {
        DrawSteeringArrows(context, drawList);

        foreach (var obj in world.Objects)
        {
            if (obj == Selection.Selected)
            {
                // Draw last
            }
            else if (obj.Kind == GameObjectKind.Ship)
            {
                var (pos, visible) = ScreenPosition(obj);

                switch (visible)
                {
                    case false when (obj.Flags & GameObjectFlags.Hostile) == GameObjectFlags.Hostile ||
                                    (obj.Flags & GameObjectFlags.Important) == GameObjectFlags.Important:
                        DrawUnselectedArrow(delta, obj, pos, context, drawList, clientRectangle);
                        break;
                    case true:
                        DrawShipReticle(delta, obj, pos, context, clientRectangle);
                        break;
                }
            }
            else if (obj.Kind == GameObjectKind.Waypoint)
            {
                var (pos, visible) = ScreenPosition(obj);

                if (visible)
                {
                    DrawWaypoint(delta, obj, pos, context, drawList, clientRectangle, false);
                    uiApi.WaypointLabel?.Draw(
                        context, delta, drawList, clientRectangle,
                        ui.PixelsToPoints(pos.X) - 45f,
                        ui.PixelsToPoints(pos.Y) - (WaypointSelectionStartSize / 2f) - 17f
                    );
                }
                else
                {
                    DrawUnselectedArrow(delta, obj, pos, context, drawList, clientRectangle);
                }
            }
            else if ((obj.Flags & GameObjectFlags.Hostile) == GameObjectFlags.Hostile ||
                     (obj.Flags & GameObjectFlags.Important) == GameObjectFlags.Important)
            {
                var (pos, visible) = ScreenPosition(obj);

                if (!visible)
                {
                    DrawUnselectedArrow(delta, obj, pos, context, drawList, clientRectangle);
                }
            }
        }

        var selected = Selection.Selected;
        if (selected == null)
        {
            selectedWaypointAnimationObject = 0;
            return;
        }

        var (selectedPos, selectedVisible) = ScreenPosition(selected);
        if (selectedVisible && selected.Kind == GameObjectKind.Waypoint)
        {
            DrawWaypoint(delta, selected, selectedPos, context, drawList, clientRectangle, true);
        }
        else
        {
            selectedWaypointAnimationObject = 0;

            if (!selectedVisible)
            {
                DrawSelectedArrow(delta, selected, selectedPos, context, drawList, clientRectangle);
            }
        }
    }

    [WattleScriptUserData]
    public class LuaAPI
    {
        private SpaceGameplay g;
        public CallbackWidget IndicatorLayer;

        public LuaAPI(SpaceGameplay gameplay)
        {
            this.g = gameplay;
            IndicatorLayer = new CallbackWidget();
        }

        private Container? lastContainer;

        public void SetIndicatorLayer(Container container)
        {
            lastContainer?.Children.Remove(IndicatorLayer);
            container.Children.Add(IndicatorLayer);
            lastContainer = container;
        }

        public UIInventoryItem[] GetScannedInventory(string filter) => g.session.GetScannedInventory(filter);
        public UIInventoryItem[] GetPlayerInventory(string filter) => g.session.GetPlayerInventory(filter);

        public Infocard? GetScannedShipInfocard()
        {
            if (g.Selection.Selected == null)
            {
                return null;
            }

            if (g.Selection.Selected.TryGetComponent<ShipComponent>(out var ship))
            {
                return g.Game.GameData.GetInfocard(ship.Ship.IdsInfo, g.Game.Fonts);
            }

            return null;
        }

        public bool CanScanSelected()
        {
            if (g.Selection.Selected == null)
            {
                return false;
            }

            return g.scanner?.CanScan(g.Selection.Selected) ?? false;
        }

        public void ScanSelected() => g.session.SpaceRpc.Scan(g.Selection.Selected!);

        public void StopScan() => g.session.SpaceRpc.StopScan();

        public Closure ScanHandler;

        public void OnUpdateScannedInventory(Closure handler)
        {
            ScanHandler = handler;
        }

        public Closure PlayerInventoryHandler;

        public void OnUpdatePlayerInventory(Closure handler)
        {
            PlayerInventoryHandler = handler;
            g.session.OnUpdateInventory = () => PlayerInventoryHandler?.Call();
        }

        public void JettisonInventoryItem(UIInventoryItem item, int count)
        {
            if (item.CanJettison)
                g.session.SpaceRpc.Jettison(item.ID, count);
        }

        public int CurrentRank => g.session.CurrentRank;
        public double NetWorth => (double)g.session.NetWorth;
        public double NextLevelWorth => (double)g.session.NextLevelWorth;
        public PlayerStats Statistics => g.session.Statistics;
        public double CharacterPlayTime => g.session.CharacterPlayTime;

        [WattleScriptHidden] public WidgetTemplate? Reticle;
        [WattleScriptHidden] public WidgetTemplate? UnselectedArrow;
        [WattleScriptHidden] public WidgetTemplate? SelectedArrow;
        [WattleScriptHidden] public WidgetTemplate? Waypoint;
        [WattleScriptHidden] public WidgetTemplate? WaypointLabel;
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

        public void SetWaypointTemplate(UiWidget template, Closure callback) =>
            Waypoint = new(template, callback);

        public void SetWaypointLabelTemplate(UiWidget template, Closure callback) =>
            WaypointLabel = new(template, callback);

        public ContactList GetContactList() => g.contactList;

        public KeyMapTable GetKeyMap()
        {
            var table = new KeyMapTable(g.Game.InputMap, g.Game.GameData.Items.Ini.Infocards);
            table.OnCaptureInput += (k) => { g.Input.KeyCapture = k; };
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

        public int CruiseCharge() => g.control.EngineState == EngineStates.CruiseCharging
            ? (int)(g.control.ChargePercent * 100)
            : -1;

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
                embeddedServer.StartFromSave(g.Game.Saves.SelectedFile!,
                    File.ReadAllBytes(g.Game.Saves.SelectedFile!));
                g.Game.ChangeState(new NetWaitState(session, g.Game));
            });
        }

        public bool CanLoadAutoSave() => !string.IsNullOrWhiteSpace(g.session.AutoSavePath);

        public void LoadAutoSave()
        {
            var path = g.session.AutoSavePath!;
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

        public Infocard? CurrentInfocard()
        {
            if (g.Selection.Selected?.SystemObject == null)
            {
                return null;
            }

            var ids = g.Selection.Selected.SystemObject.IdsInfo;
            return g.Game.GameData.GetInfocard(ids, g.Game.Fonts);
        }

        public string? CurrentInfoString() => g.Selection.Selected?.Name?.GetName(g.Game.GameData, Vector3.Zero);

        public string SelectionName()
        {
            return g.Selection.Selected?.Name?.GetName(g.Game.GameData, g.player.WorldTransform.Position) ??
                   "NULL";
        }

        public bool SelectionIsWaypoint() => g.Selection.Selected?.Kind == GameObjectKind.Waypoint;

        public string SelectionDistance()
        {
            if (g.Selection.Selected == null)
            {
                return "";
            }

            var playerPosition = g.player.WorldTransform.Position;
            var targetPosition = g.Selection.Selected.WorldTransform.Position;
            var distance = Vector3.Distance(playerPosition, targetPosition);
            return distance < 2000f
                ? $"{(int)distance}-M"
                : $"{distance / 1000f:0.0}-K";
        }

        public TargetShipWireframe? SelectionWireframe() => g.Selection.Selected != null ? g.targetWireframe : null;

        public bool SelectionVisible()
        {
            return g.Selection.Selected != null && g.ScreenPosition(g.Selection.Selected).visible;
        }

        public float SelectionHealth()
        {
            if (g.Selection.Selected == null)
            {
                return -1;
            }

            if (!g.Selection.Selected.TryGetComponent<CHealthComponent>(out var health))
            {
                return -1;
            }

            return MathHelper.Clamp(health.CurrentHealth / health.MaxHealth, 0, 1);
        }

        public float SelectionShield()
        {
            if (g.Selection.Selected == null)
            {
                return -1;
            }

            if (!g.Selection.Selected.TryGetFirstChildComponent<CShieldComponent>(out var shield))
            {
                return -1;
            }

            return shield.ShieldPercent;
        }

        public string SelectionReputation()
        {
            if (g.Selection.Selected == null)
            {
                return "neutral";
            }

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
            if (g.Selection.Selected == null)
            {
                return new Vector2(-1000, -1000);
            }

            var (pos, _) = g.ScreenPosition(g.Selection.Selected);
            return new Vector2(
                g.ui.PixelsToPoints(pos.X),
                g.ui.PixelsToPoints(pos.Y)
            );
        }

        public void PopulateNavmap(Navmap nav)
        {
            nav.PopulateIcons(g.ui, g.sys);
            nav.SetUniverse(g.Game.GameData.Items);
            nav.SetVisitFunction(g.session.IsVisited);
            nav.SetAddWaypointFunction(g.CreateUserWaypoint);
            nav.SetBestPathFunction(g.ComputeBestPathToSelection);
            nav.SetPlayerPositionProvider(() => g.player.WorldTransform.Position);
            nav.SetPlayerSystemProvider(() => g.sys.CRC);
            nav.SetUserWaypointProvider(g.session.GetUserWaypointsForNavmap);
        }

        public int UserWaypointCount() => g.session.UserWaypointCount;

        public string UserWaypointPanelText(int index) => g.session.GetUserWaypointPanelText(index);

        public void ClearUserWaypoints() => g.ClearUserWaypoints();

        public ChatSource GetChats() => g.session.Chats;
        public double GetCredits() => g.session.Credits;

        public float GetPlayerHealth() => g.playerHealth.CurrentHealth / g.playerHealth.MaxHealth;

        public float GetPlayerShield()
        {
            return g.player.GetFirstChildComponent<CShieldComponent>()?.ShieldPercent ?? -1;
        }

        public float GetPlayerPower() => g.powerCore.CurrentEnergy / g.powerCore.Equip.Capacity;

        private string activeManeuver = "FreeFlight";

        public string GetActiveManeuver() => g.pilotComponent!.CurrentBehavior switch
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
            dict.Set("Formation", g.Selection.Selected is { Kind: GameObjectKind.Ship });
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

        public int ThrustPercent() =>
            ((int)(g.powerCore.CurrentThrustCapacity / g.powerCore.Equip.ThrustCapacity * 100));

        public int Speed() => ((int)g.player.PhysicsComponent!.Body.LinearVelocity.Length());
    }
}
