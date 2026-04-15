using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer;
using LibreLancer.Client;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.MBases;
using LibreLancer.ImUI;
using LibreLancer.Resources;
using LibreLancer.Thn;

namespace LancerEdit.GameContent;

public class BaseNpcEditorTab : GameContentTab
{
    public GameDataContext Data;
    public bool Dirty;

    private MainWindow window;
    private EditorUndoBuffer undoBuffer = new();
    private PopupManager popups = new();

    // ── Data ───────────────────────────────────────────────────────────────────
    private Base[] allBases;
    private Base? selectedBase;
    private BaseRoom? selectedRoom;
    private BaseNpc? selectedNpc;

    private Viewport3D roomViewport;
    private Cutscene? roomCutscene;
    private double roomDrawElapsed;

    private ThnSceneObject[] roomSpots = [];

    // ── Camera control state ───────────────────────────────────────────────────
    private bool isRightMouseDown = false;
    private Vector2 lastMousePos = Vector2.Zero;
    private float cameraYaw = 0f;
    private float cameraPitch = 15f;
    private float cameraDistance = 40f;
    private const float MOUSE_SENSITIVITY = 0.5f;
    private const float ZOOM_SENSITIVITY = 2f;

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

    private static Regex markerRegex = new(@"^Z([sg])\/(\w+)\/(\w+)\/(\d\d)\/(\w+)\/?(\w+)?$");

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
            var script = room.SetScript.LoadScript();
            var markers = script.Entities.Values
                .Where(e => e.Type == EntityTypes.Marker &&
                            markerRegex.IsMatch(e.Name) &&
                            e.Position.HasValue)
                .Select(e => roomCutscene!.GetObject(e.Name))
                .OrderBy(x => x.Name)
                .ToArray();
            roomSpots = markers;
            roomMarkers = markers.Select(x => x.Name).ToArray();
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
            var script = previewRoom.SetScript.LoadScript();
            var ctx = ThnRoomHandler.CreateContext(selectedBase!, previewRoom);
            // SoundManager null on purpose.
            roomCutscene = new Cutscene(ctx, Data.GameData, Data.Resources, null, new Rectangle(0, 0, 240, 240), window);
            roomCutscene.BeginScene(previewRoom.OpenScene());
            roomCutscene.Update(0.1);

            roomSpots = script.Entities.Values
                .Where(e => e.Type == EntityTypes.Marker &&
                            e.Name.Contains("Zs/NPC", StringComparison.OrdinalIgnoreCase) &&
                            e.Position.HasValue)
                .Select(e => roomCutscene.GetObject(e.Name))
                .OrderBy(x => x.Name)
                .ToArray();
            roomMarkers = roomSpots.Select(x => x.Name).ToArray();
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

        if (selectedBase == null || roomSpots.Length == 0) return;

        var npcs = GetRoomNpcs().ToArray();
        var fixedNpcs = selectedRoom?.FixedNpcs
            .Where(f => f.Npc != null)
            .ToDictionary(f => f.Placement, f => f, StringComparer.OrdinalIgnoreCase);

        if (fixedNpcs != null && fixedNpcs.Count > 0)
        {
            foreach (var spot in roomSpots)
            {
                if (!fixedNpcs.TryGetValue(spot.Name, out var npc))
                    continue;

                CreatePreviewNpcObject(npc.Npc, spot, npc.FidgetScript);
            }
            return;
        }

        for (int i = 0; i < Math.Min(npcs.Length, roomSpots.Length); i++)
        {
            var npc = npcs[i];
            var spot = roomSpots[i];
            CreatePreviewNpcObject(npc, spot, null);
        }
    }

    private void CreatePreviewNpcObject(BaseNpc npc, ThnSceneObject spot, ResolvedThn? fidget)
    {
        var body = npc.Body;
        var head = npc.Head;
        var leftHand = npc.LeftHand;
        var rightHand = npc.RightHand;
        var accessory = npc.Accessory;

        if (body == null && head == null && leftHand == null && rightHand == null)
            return;

        var obj = ThnRoomHandler.AddNpc(
            roomCutscene,
            Data.Resources,
            Data.GameData.GetCharacterAnimations(),
            npc.Nickname,
            spot.Name,
            head,
            body,
            rightHand,
            leftHand,
            accessory,
            fidget
        );

        previewNpcObjects.Add(obj);
    }

    private void DrawRoomGL(int w, int h)
    {
        if (roomCutscene == null)
            return;

        // Update camera position based on yaw/pitch/distance
        UpdateCameraPosition();

        roomCutscene.Update(roomDrawElapsed);
        roomCutscene.UpdateViewport(new Rectangle(0, 0, w, h), (float)w / h);

        roomCutscene.Draw(roomDrawElapsed, w, h);
    }

    private void UpdateCameraPosition()
    {
        if (roomCutscene?.CameraHandle is not ThnCamera thnCamera) return;

        // Convert spherical to cartesian coordinates for camera position
        float yawRad = MathHelper.DegreesToRadians(cameraYaw);
        float pitchRad = MathHelper.DegreesToRadians(cameraPitch);

        float x = cameraDistance * (float)(Math.Cos(pitchRad) * Math.Sin(yawRad));
        float y = cameraDistance * (float)Math.Sin(pitchRad);
        float z = cameraDistance * (float)(Math.Cos(pitchRad) * Math.Cos(yawRad));

        var cameraPos = new Vector3(x, y, z);
        var world = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
        Matrix4x4.Invert(world, out var worldTransform);
        var transform = Transform3D.FromMatrix(worldTransform);
        thnCamera.Object!.Translate = transform.Position;
        thnCamera.Object.Rotate = transform.Orientation;
        thnCamera.Update();
    }

    private void HandleRoomViewportInput()
    {
        // Only process input if mouse is over the viewport
        if (!ImGui.IsItemHovered())
            return;

        var io = ImGui.GetIO();
        var mousePos = io.MousePos;

        // Right-click drag to rotate camera
        if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
        {
            if (isRightMouseDown)
            {
                // Calculate mouse delta
                float deltaX = mousePos.X - lastMousePos.X;
                float deltaY = mousePos.Y - lastMousePos.Y;

                // Update yaw and pitch
                cameraYaw += deltaX * MOUSE_SENSITIVITY;
                cameraPitch += deltaY * MOUSE_SENSITIVITY;

                // Clamp pitch to prevent flipping
                cameraPitch = Math.Clamp(cameraPitch, -89f, 89f);

                // Normalize yaw to 0-360
                cameraYaw = cameraYaw % 360f;
            }
            isRightMouseDown = true;
        }
        else
        {
            isRightMouseDown = false;
        }

        // Mouse wheel zoom (only while right-click is held)
        if (isRightMouseDown && Math.Abs(io.MouseWheel) > 0.001f)
        {
            cameraDistance -= io.MouseWheel * ZOOM_SENSITIVITY;
            cameraDistance = Math.Clamp(cameraDistance, 5f, 200f);
        }

        // Update last mouse position for next frame
        lastMousePos = mousePos;
    }


    private bool WorldToScreen(Vector3 world, out Vector2 screen)
    {
        screen = Vector2.Zero;
        if (roomCutscene == null)
            return false;

        var proj = roomCutscene.CameraHandle.ViewProjection;
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
        if (roomCutscene == null || roomSpots.Length == 0)
            return;

        var min = ImGui.GetItemRectMin();
        var draw = ImGui.GetWindowDrawList();

        foreach (var spot in roomSpots)
        {
            if (!WorldToScreen(spot.Translate, out var screen))
                continue;

            var p = min + screen;
            draw.AddCircleFilled(p, 6, ImGui.GetColorU32(new Vector4(1f, 0.4f, 0.1f, 0.9f)), 12);
            draw.AddText(p + new Vector2(8, -8), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.9f)), spot.Name);
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

    // ── Right-side main area ───────────────────────────────────────────────────
    private void DrawMain()
    {
        if (selectedBase != null)
        {
            var canSave = Dirty;
            if (!canSave) ImGui.BeginDisabled();
            if (ImGui.Button($"{Icons.Save} Save NPCs"))
                SaveStrategy.Save();
            if (!canSave) ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.TextDisabled(canSave ? "Unsaved changes" : "Saved");
            ImGui.Separator();
        }

        // Room selector
        DrawRoomSelector();
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
            ImGui.BeginChild("##roompreview", new Vector2(0, 260 * ImGuiHelper.Scale), ImGuiChildFlags.None, ImGuiWindowFlags.None);
            if (roomCutscene != null)
            {
                roomViewport.Draw();
                HandleRoomViewportInput();
                DrawRoomMarkerOverlay();
            }
            else
            {
                ImGui.TextDisabled("No room preview available.");
            }
            ImGui.EndChild();
            ImGui.Separator();
            if (selectedNpc != null)
                DrawNpcForm();
            else
                ImGui.TextDisabled("Select an NPC or click 'Add NPC'.");

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

        ImGui.SetNextItemWidth(220 * ImGuiHelper.Scale);
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
            int newDensity = selectedRoom.MaxCharacters;
            if (ImGui.InputInt("##roomdensity", ref newDensity) && newDensity != selectedRoom.MaxCharacters)
            {
                undoBuffer.Set("Room Character Density", () => ref selectedRoom.MaxCharacters, newDensity, () => Dirty = true);
            }
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
        var filteredNpcs = GetRoomNpcs().ToArray();
        var totalNpcs    = selectedBase!.Npcs.Count;
        bool roomFallback = filteredNpcs.Length == 0 && totalNpcs > 0;
        if (roomFallback)
            filteredNpcs = selectedBase.Npcs.ToArray();

        var npcHeader = selectedRoom != null
            ? $"{Icons.PersonRunning} NPCs ({filteredNpcs.Length}/{totalNpcs})"
            : $"{Icons.PersonRunning} NPCs ({totalNpcs})";
        ImGui.SeparatorText(npcHeader);

        if (roomFallback)
        {
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f),
                $"No NPCs assigned to room '{selectedRoom!.Nickname}' — showing all.");
        }

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
                Room           = selectedRoom?.Nickname,
            };
            undoBuffer.Commit(new ListAdd<BaseNpc>("NPC", selectedBase!.Npcs, npc));
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
        if (ImGui.BeginChild("##npclist", new Vector2(0, listH), ImGuiChildFlags.None, ImGuiWindowFlags.None))
        {
            int idx = 0;
            foreach (var npc in filteredNpcs)
            {
                ImGui.PushID(idx++);
                bool isSel = selectedNpc == npc;
                if (isSel && scrollToNpc)
                    ImGui.SetScrollHereY();

                var label = roomFallback && !string.IsNullOrEmpty(npc.Room)
                    ? $"{npc.Nickname}  [{npc.Room}]"
                    : npc.Nickname;
                if (ImGui.Selectable(label, isSel))
                    SelectNpc(npc);

                // Inline delete button
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 20 * ImGuiHelper.Scale);
                if (Controls.SmallButton($"{Icons.TrashAlt}"))
                {
                    window.Confirm($"Delete NPC '{npc.Nickname}'?", () =>
                    {
                            undoBuffer.Commit(new NpcDeleteAction(selectedBase!, npc, this));
                    });
                }
                ImGui.PopID();
            }
        }
        ImGui.EndChild();
    }

    private IReadOnlyCollection<BaseNpc> GetRoomNpcs()
    {
        if (selectedBase == null)
            return Array.Empty<BaseNpc>();

        IEnumerable<BaseNpc> npcs = selectedBase.Npcs;
        if (selectedRoom != null)
            npcs = npcs.Where(n => string.Equals(n.Room, selectedRoom.Nickname, StringComparison.OrdinalIgnoreCase));

        var fixedNpcs = selectedRoom != null
            ? selectedRoom.FixedNpcs
            : selectedBase.Rooms.SelectMany(r => r.FixedNpcs);
        npcs = npcs.Concat(fixedNpcs
            .Where(f => f.Npc != null)
            .Select(f => f.Npc!));

        return npcs.GroupBy(n => n.Nickname, StringComparer.OrdinalIgnoreCase)
                   .Select(g => g.First()).ToArray();
    }

    // ── NPC editor form ────────────────────────────────────────────────────────
    private void DrawNpcForm()
    {
        var npc = selectedNpc!;

        ImGui.BeginChild("##npcform", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.None);

        ImGui.SeparatorText($"NPC: {npc.Nickname}");

        if (Controls.BeginEditorTable("##npctable"))
        {
            // Nickname
            Controls.InputTextUndo("Nickname", undoBuffer, () => ref npc.Nickname);

            DrawBodypartField("Body",   bodyLookup,  () => ref npc.Body!);
            DrawBodypartField("Head",   headLookup,  () => ref npc.Head!);
            DrawBodypartField("L Hand", lhandLookup, () => ref npc.LeftHand!);
            DrawBodypartField("R Hand", rhandLookup, () => ref npc.RightHand!);
            DrawAccessoryListField(npc);

            // Individual Name (IDS)
            Controls.IdsInputStringUndo("Individual Name", Data, popups, undoBuffer,
                () => ref npc.IndividualName);

            // Affiliation
            Data.Factions.DrawUndo("Affiliation", undoBuffer, () => ref npc.Affiliation!, allowNull: true);

            // Voice
            Controls.InputTextUndo("Voice", undoBuffer, () => ref npc.Voice!);

            // Room
            DrawRoomAssignField(npc);

            Controls.EndEditorTable();
        }

        // ── Mission (optional) ─────────────────────────────────────────────────
        ImGui.SeparatorText("Mission (optional)");
        DrawMissionSection(npc);

        // ── Rumors ────────────────────────────────────────────────────────────
        ImGui.SeparatorText("Rumors");
        DrawRumors(npc);

        ImGui.EndChild();
    }

    // ── Field helpers ──────────────────────────────────────────────────────────

    private void DrawBodypartField(string label, ObjectLookup<Bodypart> lookup, FieldAccessor<Bodypart> accessor)
    {
        lookup.DrawUndo(label, undoBuffer, accessor, allowNull: true);
    }

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

    private void DrawRoomAssignField(BaseNpc npc)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Room");
        ImGui.TableNextColumn();

        ImGui.SetNextItemWidth(-1);
        var rooms = selectedBase!.Rooms.ToArray();
        var curRoom = npc.Room ?? "";
        if (ImGui.BeginCombo("##npcroom", curRoom))
        {
            // free-text option (keep existing)
            if (ImGui.Selectable("(none)", string.IsNullOrEmpty(curRoom)))
            {
                if (!string.IsNullOrEmpty(npc.Room))
                    undoBuffer.Set("Room", () => ref npc.Room, (string?)null);
            }
            foreach (var r in rooms)
            {
                bool isSel = string.Equals(r.Nickname, curRoom,
                    StringComparison.OrdinalIgnoreCase);
                if (ImGui.Selectable(r.Nickname, isSel))
                {
                    if (!isSel)
                        undoBuffer.Set("Room", () => ref npc.Room, r.Nickname);
                }
            }
            ImGui.EndCombo();
        }
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
