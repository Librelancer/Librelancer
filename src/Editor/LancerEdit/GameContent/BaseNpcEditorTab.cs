using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.Client;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema;
using LibreLancer.Data.Schema.MBases;
using LibreLancer.ImUI;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;
using LibreLancer.Thn;

namespace LancerEdit.GameContent;

public class BaseNpcEditorTab : GameContentTab
{
    private static readonly DropdownOption[] camModes= new[]
    {
        new DropdownOption("Arcball", Icons.Globe, CameraModes.Arcball),
        new DropdownOption("Walkthrough", Icons.StreetView, CameraModes.Walkthrough),
    };

    public GameDataContext Data;
    public bool Dirty;

    private MainWindow window;
    private EditorUndoBuffer undoBuffer = new();
    private PopupManager popups = new();
    private int randomSeed = Environment.TickCount;

    // ── Data ───────────────────────────────────────────────────────────────────
    private Base[] allBases;
    private Base? selectedBase;
    private BaseRoom? selectedRoom;
    private BaseNpc? selectedNpc;

    private Viewport3D roomViewport;
    private Cutscene? roomCutscene;
    private double roomDrawElapsed;
    private int lastWidth = 1, lastHeight = 1;
    private bool useFreeCamera = false;
    private int cameraMode = 0;
    private bool showMarkers = true;

    private (RoomNpcSpot Spot, ThnSceneObject Obj)[] roomSpots = [];

    // Bodypart lookups by category
    private ObjectLookup<Bodypart> bodyLookup;
    private ObjectLookup<Bodypart> headLookup;
    private ObjectLookup<Bodypart> lhandLookup;
    private ObjectLookup<Bodypart> rhandLookup;

    // ── Thn marker cache ───────────────────────────────────────────────────────
    private List<ThnSceneObject> previewNpcObjects = [];
    private string[] roomMarkers = [];
    private string? cachedMarkerRoom = null;

    // ── Scroll/first-frame helpers ─────────────────────────────────────────────
    private bool showHistory    = false;
    private bool scrollToNpc    = false;
    private bool firstSelected  = false;

    // ── Add-NPC form ───────────────────────────────────────────────────────────
    private string newNpcNickname = "";

    // ──────────────────────────────────────────────────────────────────────────
    public BaseNpcEditorTab(GameDataContext gameData, MainWindow mainWindow)
    {
        Title        = "Base NPC Editor";
        Data         = gameData;
        window       = mainWindow;
        SaveStrategy = new BaseNpcSaveStrategy(this);
        undoBuffer.Hook = () =>
        {
            Dirty = true;
            BuildRoomPreviewObjects();
        };

        allBases = Data.GameData.Items.Bases.OrderBy(x => x.Nickname).ToArray();

        roomViewport = new Viewport3D(mainWindow);
        roomViewport.EnableMSAA = false;
        roomViewport.Draw3D = DrawRoomGL;
        roomViewport.Mode = CameraModes.Arcball;
        roomViewport.DefaultOffset =
            roomViewport.CameraOffset = new Vector3(0, 0, 50);
        roomViewport.ModelScale = 4;
        roomViewport.ResetControls();
        roomViewport.ResetControls();

        // Build bodypart lookups grouped by path convention
        bodyLookup  = Data.Bodyparts.Filter(IsBody);
        headLookup  = Data.Bodyparts.Filter(IsHead);
        lhandLookup = Data.Bodyparts.Filter(IsLeftHand);
        rhandLookup = Data.Bodyparts.Filter(IsRightHand);
    }

    // ── Bodypart categorisation ────────────────────────────────────────────────
    private static bool PathContains(string? p, string part) =>
        p != null && p.Replace('\\', '/').Contains(part, StringComparison.OrdinalIgnoreCase);

    private static bool IsBody(Bodypart bp)     => PathContains(bp.Path, "/bodies/");
    private static bool IsHead(Bodypart bp)     => PathContains(bp.Path, "/heads/");
    private static bool IsRightHand(Bodypart bp) =>
        PathContains(bp.Path, "/hands/") &&
        bp.Nickname.Contains("right", StringComparison.OrdinalIgnoreCase);
    private static bool IsLeftHand(Bodypart bp) =>
        PathContains(bp.Path, "/hands/") &&
        bp.Nickname.Contains("left", StringComparison.OrdinalIgnoreCase);

    // ── Public helpers ─────────────────────────────────────────────────────────
    public void OnSaved() => window.OnSaved();

    public void CheckDeleted(BaseNpc npc)
    {
        if (selectedNpc == npc)
            SelectNpc(null);
    }

    // ── NPC selection ──────────────────────────────────────────────────────────
    private void SelectNpc(BaseNpc? npc)
    {
        selectedNpc = npc;
        if (npc != null)
            firstSelected = true;
    }

    // ── Thn marker loading ─────────────────────────────────────────────────────
    private void RefreshMarkers(BaseRoom room)
    {
        var key = room.Nickname + "|" + (room.SetScript?.SourcePath ?? "");
        if (key == cachedMarkerRoom) return;
        cachedMarkerRoom = key;

        if (room.SetScript == null)
        {
            roomMarkers = [];
            roomSpots = [];
            return;
        }

        try
        {
            var markers = ThnRoomHandler.GetSpots(room)
                .Select(e => (e, roomCutscene!.GetObject(e.Nickname)))
                .OrderBy(x => x.Item1.Nickname)
                .ToArray();
            roomSpots = markers;
            roomMarkers = markers.Select(x => x.Item1.Nickname).ToArray();
            BuildRoomPreviewObjects();
        }
        catch
        {
            roomMarkers = [];
            roomSpots = [];
        }
    }

    private void LoadRoomPreview(BaseRoom? room)
    {
        roomCutscene?.Dispose();
        roomCutscene = null;

        var previewRoom = room ?? selectedBase?.Rooms.FirstOrDefault() ?? selectedBase?.StartRoom;
        if (previewRoom?.SetScript == null)
        {
            roomSpots = [];
            roomMarkers = [];
            return;
        }

        try
        {
            var ctx = ThnRoomHandler.CreateContext(selectedBase!, previewRoom);
            // SoundManager null on purpose.
            roomCutscene = new Cutscene(ctx, Data.GameData, Data.Resources, null, new Rectangle(0, 0, 240, 240), window);
            roomCutscene.BeginScene(previewRoom.OpenScene());
            roomCutscene.Update(0.1);

            var markers = ThnRoomHandler.GetSpots(room)
                .Select(e => (e, roomCutscene!.GetObject(e.Nickname)))
                .OrderBy(x => x.Item1.Nickname)
                .ToArray();
            roomSpots = markers;
            roomMarkers = markers.Select(x => x.Item1.Nickname).ToArray();
            previewNpcObjects = [];
            BuildRoomPreviewObjects();
        }
        catch(Exception ex)
        {
            FLLog.Error("NPC Editor", ex.ToString());
            roomSpots = [];
            roomMarkers = [];
        }
    }



    private void BuildRoomPreviewObjects()
    {
        if (roomCutscene == null) return;

        foreach (var obj in previewNpcObjects)
        {
            roomCutscene.RemoveObject(obj);
        }

        previewNpcObjects = [];

        if (selectedBase == null || selectedRoom == null || roomSpots.Length == 0) return;

        Dictionary<string, BaseNpc> fixedNpcs = new();
        HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);

        foreach (var n in selectedRoom.Npcs)
        {
            if (n.Placement != null)
            {
                fixedNpcs[n.Placement.Spot] = n;
                used.Add(n.Placement.Spot);
            }
        }

        foreach (var spot in roomSpots)
        {
            if (!fixedNpcs.TryGetValue(spot.Obj.Name, out var npc))
                continue;

            CreatePreviewNpcObject(npc, spot.Obj, npc.Placement!.FidgetScript);
        }

        var dynSpots = roomSpots.Where(x => x.Spot.Dynamic).ToArray();
        var npcs = selectedRoom.Npcs.Where(x => x.Placement == null).ToArray();

        var r = new Random(randomSeed);
        r.Shuffle(dynSpots);
        r.Shuffle(npcs);

        int spawnCount = 0;
        for (int i = 0; i < dynSpots.Length; i++)
        {
            if (spawnCount >= npcs.Length || spawnCount >= selectedRoom.MaxCharacters)
                break;
            if (used.Contains(dynSpots[i].Spot.Nickname))
                continue;
            var spot = dynSpots[i];
            var npc = npcs[spawnCount];
            var fidgetScripts = Data.GameData.Items.GetGCSScripts("fidget",
                npc.Body?.Sex ?? FLGender.male, spot.Spot.Posture);
            CreatePreviewNpcObject(npc, spot.Obj, fidgetScripts[0]);
            spawnCount++;
        }
    }

    private void CreatePreviewNpcObject(BaseNpc npc, ThnSceneObject spot, ResolvedThn? fidget)
    {
        var body = npc.Body ?? npc.BaseAppr?.Body;
        var head = npc.Head ?? npc.BaseAppr?.Head;
        var leftHand = npc.LeftHand ?? npc.BaseAppr?.LeftHand;
        var rightHand = npc.RightHand ?? npc.BaseAppr?.RightHand;
        var accessory = npc.Accessory ?? npc.BaseAppr?.Accessory;

        if (body == null && head == null && leftHand == null && rightHand == null)
            return;

        FLLog.Debug("NPC", $"Spawning {npc.Nickname}");
        var existing = roomCutscene.GetObject(npc.Nickname);
        if (existing != null)
        {
            if (previewNpcObjects.Contains(existing))
            {
                FLLog.Error("NPC Editor", "Duplicate npc!");
                return;
            }
            FLLog.Warning("NPC Editor", "Object nickname clash");
            roomCutscene.RemoveObject(existing);
        }

        var obj = ThnRoomHandler.AddNpc(
            roomCutscene,
            Data.Resources,
            Data.GameData.GetCharacterAnimations(),
            npc.Nickname,
            spot.Name,
            npc.Voice,
            head,
            body,
            rightHand,
            leftHand,
            accessory,
            fidget
        );

        previewNpcObjects.Add(obj);
    }

    LookAtCamera? GetFreeCamera(int renderWidth, int renderHeight)
    {
        if (!useFreeCamera)
            return null;
        var cam = new LookAtCamera();
        Matrix4x4 rot = Matrix4x4.CreateRotationX(roomViewport.CameraRotation.Y) *
                        Matrix4x4.CreateRotationY(roomViewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = roomViewport.CameraOffset + (dir * 10);
        if (roomViewport.Mode == CameraModes.Arcball)
            to = Vector3.Zero;
        cam.Update(renderWidth, renderHeight, roomViewport.CameraOffset, to, rot);
        return cam;
    }

    private void DrawRoomGL(int w, int h)
    {
        if (roomCutscene == null)
            return;
        lastWidth = w;
        lastHeight = h;
        // Update camera position based on yaw/pitch/distance
        UpdateThnCamera();

        roomCutscene.Update(roomDrawElapsed);
        roomCutscene.UpdateViewport(new Rectangle(0, 0, w, h), (float)w / h);

        roomCutscene.Draw(roomDrawElapsed, w, h, GetFreeCamera(w, h));
    }


    private void UpdateThnCamera()
    {
        if (roomCutscene == null || selectedRoom == null)
            return;

        var cam = selectedRoom.Camera;
        if (cam == null)
            cam = roomCutscene.AllObjects.FirstOrDefault(x => x.Camera != null)?.Name;
        if(cam != null)
            roomCutscene.SetCamera(cam);
    }


    private bool WorldToScreen(Vector3 world, out Vector2 screen, LookAtCamera? freeCam)
    {
        screen = Vector2.Zero;
        if (roomCutscene == null)
            return false;

        var proj = freeCam?.ViewProjection ?? roomCutscene.CameraHandle.ViewProjection;
        var clip = Vector4.Transform(new Vector4(world, 1), proj);
        if (Math.Abs(clip.W) < float.Epsilon)
            return false;

        clip /= clip.W;
        if (clip.X < -1 || clip.X > 1 || clip.Y < -1 || clip.Y > 1 || clip.Z < 0 || clip.Z > 1)
            return false;

        screen = new Vector2(
            (clip.X * 0.5f + 0.5f) * roomViewport.ControlWidth,
            (1f - (clip.Y * 0.5f + 0.5f)) * roomViewport.ControlHeight);
        return true;
    }


    private void DrawRoomMarkerOverlay()
    {
        if (roomCutscene == null || roomSpots.Length == 0 || !showMarkers)
            return;

        var freeCam = GetFreeCamera(lastWidth, lastHeight);

        var min = ImGui.GetItemRectMin();
        var draw = ImGui.GetWindowDrawList();

        foreach (var spot in roomSpots)
        {
            if (!WorldToScreen(spot.Obj.Translate, out var screen, freeCam))
                continue;

            var p = min + screen;
            draw.AddCircleFilled(p, 6, ImGui.GetColorU32(new Vector4(1f, 0.4f, 0.1f, 0.9f)), 12);
            draw.AddText(p + new Vector2(8, -8), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.9f)), spot.Obj.Name);
        }
    }

    // ── Hotkey support ─────────────────────────────────────────────────────────
    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        if (hk == Hotkeys.Undo && undoBuffer.CanUndo) undoBuffer.Undo();
        if (hk == Hotkeys.Redo && undoBuffer.CanRedo) undoBuffer.Redo();
    }


    // ── Main draw ──────────────────────────────────────────────────────────────
    public override void Draw(double elapsed)
    {
        var sz = ImGui.GetContentRegionAvail();
        sz.Y -= 3 * ImGuiHelper.Scale;

        if (ImGui.BeginTable("##root", 2,
            ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit |
            ImGuiTableFlags.NoHostExtendY, sz))
        {
            ImGui.TableSetupColumn("#sidebar", ImGuiTableColumnFlags.WidthFixed,
                200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("#main", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawSidebar();

            ImGui.TableNextColumn();
            if (selectedBase != null)
            {
                roomDrawElapsed = elapsed;
                DrawMain();
            }
            else
                ImGui.TextDisabled("Select a base to begin.");

            ImGui.EndTable();
        }

        if (showHistory)
            undoBuffer.DisplayStack();

        popups.Run();
        firstSelected  = false;
        scrollToNpc    = false;
    }

    // ── Sidebar: base list ─────────────────────────────────────────────────────
    private void DrawSidebar()
    {
        ImGui.TextDisabled("Bases");
        ImGui.Separator();

        if (ImGuiExt.ToggleButton("History", showHistory))
            showHistory = !showHistory;

        ImGui.BeginChild("##bases_list", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.None);
        int i = 0;
        foreach (var b in allBases)
        {
            ImGui.PushID(i++);
            bool isSel = selectedBase == b;
            if (ImGui.Selectable($"{b.Nickname}", isSel))
            {
                if (selectedBase != b)
                {
                    selectedBase  = b;
                    selectedRoom  = selectedBase.Rooms.FirstOrDefault() ?? selectedBase.StartRoom;
                    selectedNpc   = null;
                    cachedMarkerRoom = null;
                    LoadRoomPreview(selectedRoom);
                }
            }
            ImGui.PopID();
        }
        ImGui.EndChild();
    }

    private float h1 = 200, h2 = 200;


    // ── Right-side main area ───────────────────────────────────────────────────
    private void DrawMain()
    {
        roomViewport.SetInputsEnabled(roomCutscene != null && useFreeCamera);

        // Room selector
        DrawRoomSelector();
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.SyncAlt} Refresh"))
        {
            randomSeed = new Random(randomSeed).Next();
            LoadRoomPreview(selectedRoom);
        }
        ImGui.SetItemTooltip("Regenerates the random NPC positioning");
        ImGui.SameLine();
        ImGui.Checkbox("Draw Labels", ref showMarkers);
        ImGui.SameLine();
        ImGui.Checkbox("Free Cam", ref useFreeCamera);
        ImGui.SameLine();
        ImGui.BeginDisabled(!useFreeCamera);
        ImGuiExt.DropdownButton("Camera Mode", ref cameraMode, camModes);
        roomViewport.Mode = (CameraModes) camModes[cameraMode].Tag;
        ImGui.SameLine();
        if (ImGui.Button("Reset Camera"))
            roomViewport.ResetControls();
        ImGui.EndDisabled();
        ImGui.Separator();


        var avail = ImGui.GetContentRegionAvail();

        // Split: left = marker list + NPC list, right = NPC form
        if (ImGui.BeginTable("##mainsplit", 2,
            ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable |
            ImGuiTableFlags.NoHostExtendY, avail))
        {
            ImGui.TableSetupColumn("#left",  ImGuiTableColumnFlags.WidthStretch, 0.30f);
            ImGui.TableSetupColumn("#right", ImGuiTableColumnFlags.WidthStretch, 0.70f);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawLeftPane();
            ImGui.TableNextColumn();
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref h1, ref h2, 8, 8, -1);
            h1 = totalH - h2 - 24f;
            ImGui.BeginChild("##roompreview", new Vector2(-1, h1), ImGuiChildFlags.None, ImGuiWindowFlags.None);
            if (roomCutscene != null)
            {
                roomViewport.Draw();
                DrawRoomMarkerOverlay();
            }
            else
            {
                ImGui.TextDisabled("No room preview available.");
            }
            ImGui.EndChild();

            ImGui.BeginChild("##npcform", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.None);
            if (selectedNpc != null)
                DrawNpcForm();
            else
                ImGui.TextDisabled("Select an NPC or click 'Add NPC'.");
            ImGui.EndChild();

            ImGui.EndTable();
        }
    }

    // ── Room dropdown ──────────────────────────────────────────────────────────
    private void DrawRoomSelector()
    {
        if (selectedBase == null)
        {
            ImGui.TextDisabled("No base selected");
            return;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text($"{Icons.PersonRunning} Room:");
        ImGui.SameLine();

        var rooms = selectedBase.Rooms.ToArray();
        var currentLabel = selectedRoom?.Nickname ?? selectedBase.StartRoom?.Nickname ?? "(none)";

        ImGui.SetNextItemWidth(140 * ImGuiHelper.Scale);
        if (ImGui.BeginCombo("##room", currentLabel))
        {
            for (int i = 0; i < rooms.Length; i++)
            {
                bool sel = selectedRoom == rooms[i];
                if (ImGui.Selectable(rooms[i].Nickname, sel))
                {
                    if (!sel)
                    {
                        selectedRoom = rooms[i];
                        cachedMarkerRoom = null;
                        LoadRoomPreview(selectedRoom);
                    }
                }
                if (sel) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        RefreshMarkers(selectedRoom ?? selectedBase.Rooms.FirstOrDefault() ?? selectedBase.StartRoom);
        if (selectedRoom != null)
        {
            ImGui.SameLine();
            ImGui.Text("Density:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(70 * ImGuiHelper.Scale);
            Controls.InputIntValueUndo("Density", undoBuffer, () => ref selectedRoom.MaxCharacters,
                1, 1, ImGuiInputTextFlags.None, new(0, 1000));
        }
        ImGui.SameLine();
        ImGui.TextDisabled($"({roomMarkers.Length} NPC spots found)");
    }

    // ── Left pane: markers + NPC list ─────────────────────────────────────────
    private void DrawLeftPane()
    {
        var paneH = ImGui.GetContentRegionAvail().Y;

        // NPC spot markers
        if (roomMarkers.Length > 0)
        {
            ImGui.SeparatorText($"{Icons.Map} NPC Spots");
            float markerH = Math.Min(roomMarkers.Length * ImGui.GetTextLineHeightWithSpacing() + 8,
                                     paneH * 0.35f);
            ImGui.BeginChild("##markers", new Vector2(0, markerH), ImGuiChildFlags.Borders, ImGuiWindowFlags.None);
            foreach (var m in roomMarkers)
                ImGui.Text(m);
            ImGui.EndChild();
        }

        // NPC list
        ImGui.SeparatorText($"{Icons.PersonRunning} NPCs ({selectedRoom.Npcs.Count})");

        // Add-NPC row
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (70 * ImGuiHelper.Scale));
        Controls.InputTextFilter("##newnick", ref newNpcNickname, Controls.IdFilter);
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.PlusCircle}##addnpc") && !string.IsNullOrWhiteSpace(newNpcNickname))
        {
            var npc = new BaseNpc(newNpcNickname)
            {
                Nickname       = newNpcNickname,
                Voice          = "rvp106", // I don't know yet how to extract the list of possible voices, a default value and a text box should suffice.
            };
            undoBuffer.Commit(new ListAdd<BaseNpc>("NPC", selectedRoom!.Npcs, npc));
            newNpcNickname = "";
            window.QueueUIThread(() =>
            {
                SelectNpc(npc);
                scrollToNpc = true;
            });
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Add new NPC (type nickname first)");

        // List
        float listH = ImGui.GetContentRegionAvail().Y - 3 * ImGuiHelper.Scale;
        var itemH = ImGui.CalcTextSize("|").Y;
        if (ImGui.BeginChild("##npclist", new Vector2(0, listH), ImGuiChildFlags.None, ImGuiWindowFlags.None))
        {
            int idx = 0;
            var w = ImGui.GetWindowWidth();
            foreach (var npc in selectedRoom.Npcs)
            {
                ImGui.PushID(idx++);
                bool isSel = selectedNpc == npc;
                if (isSel && scrollToNpc)
                    ImGui.SetScrollHereY();
                var sz = new Vector2(w - Controls.ButtonWidth($"{Icons.TrashAlt}"), itemH);
                if (ImGui.Selectable(npc.Nickname, isSel, 0, sz))
                    SelectNpc(npc);

                // Inline delete button
                ImGui.SameLine();
                if (Controls.SmallButton($"{Icons.TrashAlt}"))
                {
                    window.Confirm($"Delete NPC '{npc.Nickname}'?", () =>
                    {
                        undoBuffer.Commit(new NpcDeleteAction(selectedRoom!, npc, this));
                    });
                }
                ImGui.PopID();
            }
        }
        ImGui.EndChild();
    }


    // ── NPC editor form ────────────────────────────────────────────────────────
    private void DrawNpcForm()
    {
        var npc = selectedNpc!;

        ImGui.SeparatorText($"NPC: {npc.Nickname}");

        if (Controls.BeginEditorTable("##npctable"))
        {
            // Nickname
            Controls.InputTextUndo("Nickname", undoBuffer, () => ref npc.Nickname);

            Data.Costumes.DrawUndo("Base Appr", undoBuffer, () => ref npc.BaseAppr, true);
            bodyLookup.DrawUndo("Body", undoBuffer, () => ref npc.Body, npc.BaseAppr?.Body != null);
            headLookup.DrawUndo("Head",   undoBuffer,  () => ref npc.Head, true);
            lhandLookup.DrawUndo("L Hand", undoBuffer, () => ref npc.LeftHand, true);
            rhandLookup.DrawUndo("R Hand", undoBuffer, () => ref npc.RightHand, true);
            DrawAccessoryListField(npc);

            // Individual Name (IDS)
            Controls.IdsInputStringUndo("Individual Name", Data, popups, undoBuffer,
                () => ref npc.IndividualName);

            // Affiliation
            Data.Factions.DrawUndo("Affiliation", undoBuffer, () => ref npc.Affiliation!, allowNull: true);

            // Voice
            Controls.InputTextUndo("Voice", undoBuffer, () => ref npc.Voice!);

            // Placement
            if (npc.Placement == null)
            {
                Controls.EditControlSetup("Placement", 0);
                if (ImGui.Button($"Assign", new(ImGui.CalcItemWidth(), 0)))
                {
                    popups.OpenPopup(new NpcPlacementPopup(npc, roomSpots.Select(x => x.Spot).ToArray(), Data,
                        x => undoBuffer.Set("Placement", () => ref npc.Placement, x)));
                }
            }
            else
            {
                var btnWidths = -(Controls.ButtonWidth($"{Icons.Edit}") + Controls.ButtonWidth($"{Icons.TrashAlt}"));
                Controls.EditControlSetup("Placement", 0, btnWidths);
                ImGui.LabelText("", npc.Placement.ToString());
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.Edit}##placement"))
                {
                    popups.OpenPopup(new NpcPlacementPopup(npc, roomSpots.Select(x => x.Spot).ToArray(), Data,
                        x => undoBuffer.Set("Placement", () => ref npc.Placement, x)));
                }
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.TrashAlt}##placement"))
                {
                    undoBuffer.Set("Placement", () => ref npc.Placement, null);
                }
            }
            Controls.EndEditorTable();
        }

        // ── Mission (optional) ─────────────────────────────────────────────────
        ImGui.SeparatorText("Mission (optional)");
        DrawMissionSection(npc);

        // ── Rumors ────────────────────────────────────────────────────────────
        ImGui.SeparatorText("Rumors");
        DrawRumors(npc);

    }

    // ── Field helpers ──────────────────────────────────────────────────────────

    private void DrawAccessoryListField(BaseNpc npc)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Accessory");
        ImGui.TableNextColumn();

        int removeIndex = -1;
        Accessory removeValue = null!;

        bool wasInTable = Controls.InEditorTable;
        Controls.InEditorTable = false;
        for (int i = 0; i < npc.Accessories.Count; i++)
        {
            var idx = i;
            var current = npc.Accessories[i];
            ImGui.PushID(i);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (80 * ImGuiHelper.Scale));
            Data.Accessories.Draw($"##acc_{i}", ref current, (old, updated) =>
                undoBuffer.Commit(new ListSet<Accessory>("Accessory", npc.Accessories, idx, old, updated)));
            ImGui.SameLine();
            if (Controls.SmallButton($"{Icons.TrashAlt}##rmacc"))
            {
                removeIndex = i;
                removeValue = current;
            }
            ImGui.PopID();
        }
        Controls.InEditorTable = wasInTable;

        if (npc.Accessories.Count == 0)
        {
            ImGui.TextDisabled("(none)");
            ImGui.SameLine();
        }
        else
        {
            ImGui.SameLine(0, 4 * ImGuiHelper.Scale);
        }

        if (Controls.SmallButton($"{Icons.PlusCircle} Add##addacc"))
        {
            var first = Data.GameData.Items.Accessories.FirstOrDefault();
            if (first != null)
                undoBuffer.Commit(new ListAdd<Accessory>("Accessory", npc.Accessories, first));
        }

        if (removeIndex >= 0)
            undoBuffer.Commit(new ListRemove<Accessory>("Accessory", npc.Accessories, removeIndex, removeValue));
    }

    // ── Mission section ────────────────────────────────────────────────────────
    private void DrawMissionSection(BaseNpc npc)
    {
        if (npc.Mission != null)
        {
            if (Controls.BeginEditorTable("misn"))
            {
                Controls.InputFloatUndo("Min", undoBuffer, () => ref npc.Mission.Min);
                Controls.InputFloatUndo("Max", undoBuffer, () => ref npc.Mission.Max);
                Controls.EndEditorTable();
            }
            if (ImGui.Button($"{Icons.TrashAlt} Clear Mission"))
                undoBuffer.Set("Mission", () => ref npc.Mission, (NpcMission?)null);
        }
        else
        {
            if (ImGui.Button($"{Icons.PlusCircle} Add Mission"))
            {
                undoBuffer.Set("Mission", () => ref npc.Mission, new NpcMission("DestroyMission", 0f, 0.98f));
            }
        }
    }

    // ── Rumor list ─────────────────────────────────────────────────────────────
    private void DrawRumors(BaseNpc npc)
    {
        const ImGuiTableFlags tableFlags =
            ImGuiTableFlags.BordersOuter |
            ImGuiTableFlags.BordersInnerV |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.SizingStretchProp;

        if (ImGui.BeginTable("##rumortable", 5, tableFlags))
        {
            ImGui.TableSetupColumn("Start",  ImGuiTableColumnFlags.WidthStretch, 2f);
            ImGui.TableSetupColumn("End",    ImGuiTableColumnFlags.WidthStretch, 2f);
            ImGui.TableSetupColumn("Rep",    ImGuiTableColumnFlags.WidthFixed,   50 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("IDS",    ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableSetupColumn("##del",  ImGuiTableColumnFlags.WidthFixed,   ImGui.GetFrameHeight());
            ImGui.TableHeadersRow();

            int removeIdx = -1;
            int idx = 0;
            foreach (var r in npc.Rumors)
            {
                ImGui.TableNextRow();
                ImGui.PushID(idx);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var start = r.Start;
                Data.StoryIndices.Draw("##start", ref start, (o, u) =>
                    undoBuffer.Set("Rumor Start", () => ref r.Start!, o, u), allowNull: true);
                r.Start = start;

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var end = r.End;
                Data.StoryIndices.Draw("##end", ref end, (o, u) =>
                    undoBuffer.Set("Rumor End", () => ref r.End!, o, u), allowNull: true);
                r.End = end;

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int rep = r.RepRequired;
                if (ImGui.InputInt("##rep", ref rep, 0) && rep != r.RepRequired)
                    undoBuffer.Set("Rumor Rep", () => ref r.RepRequired, rep);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                Controls.IdsInputXmlUndo("##ids", window, Data, popups, undoBuffer,
                    () => ref r.Ids, showTooltipOnHover: true, inputWidth: 0f);

                ImGui.TableNextColumn();
                if (Controls.SmallButton($"{Icons.TrashAlt}##del"))
                    removeIdx = idx;

                ImGui.PopID();
                idx++;
            }

            ImGui.EndTable();

            if (removeIdx >= 0)
                undoBuffer.Commit(new ListRemove<BaseNpcRumor>("Rumor", npc.Rumors, removeIdx, npc.Rumors[removeIdx]));
        }

        if (npc.Rumors.Count == 0)
            ImGui.TextDisabled("No rumors.");

        if (ImGui.Button($"{Icons.PlusCircle} Add Rumor"))
        {
            var first = Data.GameData.Items.Story.FirstOrDefault();
            undoBuffer.Commit(new ListAdd<BaseNpcRumor>("Rumor", npc.Rumors, new BaseNpcRumor
            {
                RepRequired = 1,
                Start = first,
                End = first
            }));
        }
    }

    public override void Dispose()
    {
        roomCutscene?.Dispose();
        roomViewport.Dispose();
    }
}
