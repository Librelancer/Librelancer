using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Groups;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.Schema.Universe;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Interface;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public class EditMap2D
{
    public static int MarginH = (int)(40 * ImGuiHelper.Scale);
    public static int MarginW = (int)(15 * ImGuiHelper.Scale);

    private const float GridSizeDefault = 240000;
    private static readonly string[] GRIDNUMBERS = { "1", "2", "3", "4", "5", "6", "7", "8" };
    private static readonly string[] GRIDLETTERS = { "A", "B", "C", "D", "E", "F", "G", "H" };
    private static readonly int GRID_DIVISIONS = 8;

    private static readonly uint MAP_BG_COLOUR = 0xFF1C0812;
    private static readonly uint CLUSTER_BG_COLOUR = 0xFF8888FF;
    private static readonly uint TRADELANE_DESELECTED_COLOUR = 0xFF454770;
    private static readonly uint SELECTED_COLOUR = 0xFFFFB366;
    private static readonly uint COLOUR_WHITE = 0xFFFFFFFF;

    private static readonly float ZOOM_SPEED = 0.15f;
    private static readonly float MIN_ZOOM = 1f;
    private static readonly float MAX_ZOOM = 100f;
    private Vector2 CameraCenter = new(0.5f, 0.5f); // normalized (0..1)

    public float Zoom = 1;
    private float lastZoom = 1f;

    private enum MapLod { Minimal, Reduced, Detailed }

    GameObject dragTarget;
    Transform3D dragOriginalTransform;
    RenderContext renderContext;
    List<ObjectCluster> clusters = new();
    List<GameObject> currentClusterObjects;
    List<TradeLaneGroup> groupedTradelanes = new();
    TradeLaneGroup selectedTradeLaneGroup = new();

    TradeLaneGroup draggingTradeLaneGroup;
    Dictionary<GameObject, Transform3D> tradeLaneDragStartTransforms;
    Vector2 tradeLaneDragStartMouse;

    // Creation tools (patrols, zones)
    public Map2DCreationTools CreationTools { get; } = new();

    public void Draw(SystemEditData system, GameWorld world, GameDataContext ctx, SystemEditorTab tab, RenderContext renderContext)
    {
        groupedTradelanes = new TradeLaneGrouper()
            .Build(tab.ObjectsList.Objects);

        // Reserve stable layout space (CRITICAL: prevents scrolling bug)
        Vector2 canvasPos = ImGui.GetCursorScreenPos();
        Vector2 canvasSize = new(
            Math.Max(120, ImGui.GetWindowWidth() - MarginW),
            Math.Max(120, ImGui.GetWindowHeight() - MarginH)
        );

        ImGui.Dummy(canvasSize);

        Vector2 canvasMin = canvasPos;
        Vector2 canvasMax = canvasPos + canvasSize;

        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(canvasMin, canvasMax, true);

        Vector2 viewportCenter = canvasMin + canvasSize * 0.5f;

        // Zoom + camera
        if (Zoom <= 1.0f)
        {
            Zoom = 1.0f;
            CameraCenter = new Vector2(0.5f, 0.5f);
        }

        float baseMapSize = Math.Min(canvasSize.X, canvasSize.Y);
        float mapSize = baseMapSize * Zoom;

        float halfVisible = 0.5f / Zoom;
        CameraCenter.X = Math.Clamp(CameraCenter.X, halfVisible, 1f - halfVisible);
        CameraCenter.Y = Math.Clamp(CameraCenter.Y, halfVisible, 1f - halfVisible);

        Vector2 mapTopLeft = viewportCenter - (CameraCenter * mapSize);

        // Background
        drawList.AddRectFilled(mapTopLeft, mapTopLeft + new Vector2(mapSize), MAP_BG_COLOUR);

        drawList.AddRect(mapTopLeft, mapTopLeft + new Vector2(mapSize), COLOUR_WHITE, 0, ImDrawFlags.None, 2f);

        // Grid + labels
        DrawGridAndLabelsViewportAware(drawList, canvasMin, canvasSize, mapTopLeft, mapSize);

        // LOD
        MapLod lod =
            Zoom < 4f ? MapLod.Minimal :
            Zoom < 10f ? MapLod.Reduced :
                        MapLod.Detailed;
        DrawZones(system, tab, ctx, mapTopLeft, mapSize, drawList, lod);
        DrawTradeLaneLinesLOD(system, tab, ctx, mapTopLeft, mapSize, drawList, lod);
        DrawTradeLaneIconsLOD(system, tab, ctx, mapTopLeft, mapSize, drawList, lod);
        DrawObjectsLOD(system, tab, ctx, mapTopLeft, mapSize, drawList, lod);
        UpdateTradeLaneGroupDrag(system, mapSize, tab);

        // Pan
        if (ImGui.IsWindowHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
        {
            Vector2 delta = ImGui.GetIO().MouseDelta;
            CameraCenter -= delta / mapSize;
        }
        // Zoom
        else if (ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel != 0f)
        {
            float wheel = ImGui.GetIO().MouseWheel;
            float oldZoom = Zoom;

            Zoom *= MathF.Exp(wheel * ZOOM_SPEED);
            Zoom = Math.Clamp(Zoom, MIN_ZOOM, MAX_ZOOM);

            if (Zoom != oldZoom)
            {
                Vector2 mouseScreen = ImGui.GetIO().MousePos;

                Vector2 mouseMapBefore =
                    (mouseScreen - mapTopLeft) / mapSize;

                float newMapSize = baseMapSize * Zoom;

                Vector2 mouseMapAfter =
                    (mouseScreen - viewportCenter) / newMapSize + CameraCenter;

                CameraCenter += (mouseMapBefore - mouseMapAfter);
            }
        }

        drawList.PopClipRect();

        // Context menu (anchored to Dummy, NOT cursor movement)
        DrawContextMenu(system, world, ctx, tab, mapSize, mapTopLeft);

        // Creation tools
        var helpText = CreationTools.Draw(
            drawList,
            mapTopLeft,
            mapSize,
            world => WorldToMap_Local(world, system, mapSize),
            map => MapToWorld_Local(map, system, mapSize),
            tab
        );

        // Cluster popup
        if (ImGui.BeginPopup("##clusterPopup"))
        {
            if (currentClusterObjects != null)
            {
                foreach (var obj in currentClusterObjects)
                {
                    if (ImGui.Selectable(obj.Nickname))
                    {
                        tab.ForceSelectObject(obj);
                        ImGui.CloseCurrentPopup();
                    }
                }
            }
            ImGui.EndPopup();
        }
    }

    void DrawGridAndLabelsViewportAware(ImDrawListPtr drawList, Vector2 viewportPos, Vector2 viewportSize, Vector2 mapTopLeft, float mapSize)
    {
        Vector2 viewportMin = viewportPos;
        Vector2 viewportMax = viewportPos + viewportSize;

        // Convert viewport corners to normalized map space (0..1)
        Vector2 mapMin = (viewportMin - mapTopLeft) / mapSize;
        Vector2 mapMax = (viewportMax - mapTopLeft) / mapSize;

        mapMin = Vector2.Clamp(mapMin, Vector2.Zero, Vector2.One);
        mapMax = Vector2.Clamp(mapMax, Vector2.Zero, Vector2.One);

        int colMin = Math.Clamp((int)Math.Floor(mapMin.X * GRID_DIVISIONS), 0, GRID_DIVISIONS - 1);
        int colMax = Math.Clamp((int)Math.Floor(mapMax.X * GRID_DIVISIONS), 0, GRID_DIVISIONS - 1);
        int rowMin = Math.Clamp((int)Math.Floor(mapMin.Y * GRID_DIVISIONS), 0, GRID_DIVISIONS - 1);
        int rowMax = Math.Clamp((int)Math.Floor(mapMax.Y * GRID_DIVISIONS), 0, GRID_DIVISIONS - 1);

        float cellSize = mapSize / GRID_DIVISIONS;
        uint gridColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.4f));

        // ---- Grid lines (map space) ----
        for (int i = 1; i < GRID_DIVISIONS; i++)
        {
            float p = i * cellSize;

            drawList.AddLine(
                mapTopLeft + new Vector2(p, 0),
                mapTopLeft + new Vector2(p, mapSize),
                gridColor,
                1f
            );

            drawList.AddLine(
                mapTopLeft + new Vector2(0, p),
                mapTopLeft + new Vector2(mapSize, p),
                gridColor,
                1f
            );
        }

        // ---- Labels (viewport space) ----
        ImGui.PushFont(ImGuiHelper.SystemMonospace, 12f);

        // Column labels (top)
        for (int col = colMin; col <= colMax; col++)
        {
            float mapX = (col + 0.5f) / GRID_DIVISIONS;
            float screenX = mapTopLeft.X + mapX * mapSize;

            if (screenX < viewportMin.X || screenX > viewportMax.X)
                continue;

            string label = GRIDLETTERS[col];
            Vector2 size = ImGui.CalcTextSize(label);

            drawList.AddText(
                new Vector2(screenX - size.X * 0.5f, viewportMin.Y + 4),
                0xFFFFFFFF,
                label
            );
        }

        // Row labels (left)
        for (int row = rowMin; row <= rowMax; row++)
        {
            float mapY = (row + 0.5f) / GRID_DIVISIONS;
            float screenY = mapTopLeft.Y + mapY * mapSize;

            if (screenY < viewportMin.Y || screenY > viewportMax.Y)
                continue;

            string label = GRIDNUMBERS[row];
            Vector2 size = ImGui.CalcTextSize(label);

            float labelX = Math.Max(viewportMin.X, mapTopLeft.X) + 4;

            drawList.AddText(
                new Vector2(labelX, screenY - size.Y * 0.5f),
                0xFFFFFFFF,
                label
            );
        }

        ImGui.PopFont();
    }
    void DrawZones(SystemEditData system, SystemEditorTab tab, GameDataContext ctx, Vector2 mapTopLeft, float mapSize, ImDrawListPtr drawList, MapLod lod)
    {
        foreach (var z in tab.ZoneList.Zones)
        {
            if (!z.Visible)
                continue;

            VertexDiffuse col = new VertexDiffuse();
            switch (tab.ZoneList.GetZoneType(z.Current.Nickname))
            {
                case ZoneDisplayKind.Normal:
                    col = (VertexDiffuse)Color4.Pink.ChangeAlpha(0.33f);
                    break;
                case ZoneDisplayKind.ExclusionZone:
                    col = (VertexDiffuse)Color4.Red.ChangeAlpha(0.33f);
                    break;
                case ZoneDisplayKind.AsteroidField:
                    col = (VertexDiffuse)Color4.Orange.ChangeAlpha(0.33f);
                    break;
                case ZoneDisplayKind.Nebula:
                    col = (VertexDiffuse)Color4.LightGreen.ChangeAlpha(0.33f);
                    break;
                default:
                    break;
            }

            // ----- Filled mesh -----
            var mesh = z.Current.TopDownMesh();
            var verts = ArrayPool<Vector2>.Shared.Rent(mesh.Length);

            for (int i = 0; i < mesh.Length; i++)
            {
                var world = z.Current.Position + new Vector3(mesh[i].X, 0, mesh[i].Y);
                verts[i] = WorldToScreen(world, system, mapTopLeft, mapSize);
            }

            drawList.AddTriangleMesh(
                verts,
                mesh.Length,
                col
            );

            // ----- Outline -----
            mesh = z.Current.OutlineMesh();
            for (int i = 0; i < mesh.Length; i++)
            {
                var world = z.Current.Position + new Vector3(mesh[i].X, 0, mesh[i].Y);
                verts[i] = WorldToScreen(world, system, mapTopLeft, mapSize);
            }

            drawList.AddPolyline(
                ref verts[0],
                mesh.Length,
                ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.51f)),
                ImDrawFlags.None,
                1f
            );

            Vector2 center = PolygonCentroid(verts.AsSpan(0, mesh.Length));

            string label = z.Current.Nickname; // or DisplayName
            Vector2 textSize = ImGui.CalcTextSize(label);

            drawList.AddText(
                center - textSize * 0.5f,
                ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)),
                label
            );
            ArrayPool<Vector2>.Shared.Return(verts);
        }
    }
    void DrawObjectsLOD(SystemEditData system, SystemEditorTab tab, GameDataContext ctx, Vector2 mapTopLeft, float mapSize, ImDrawListPtr drawList, MapLod lod)
    {
        float clusterRadius = lod == MapLod.Minimal
            ? 30f : lod == MapLod.Reduced
            ? 15f : 5f;

        clusters.Clear();

        // ---- Build clusters ----
        foreach (var obj in tab.ObjectsList.Objects)
        {
            if (IsTradeLaneRing(obj))
                continue;

            Vector2 screen = WorldToScreen(
                obj.LocalTransform.Position,
                system,
                mapTopLeft,
                mapSize
            );

            // Tradelanes NEVER cluster
            if (clusterRadius > 0)
            {
                bool added = false;
                foreach (var c in clusters)
                {
                    if (Vector2.DistanceSquared(c.ScreenPos, screen) <
                        clusterRadius * clusterRadius)
                    {
                        c.Objects.Add(obj);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    clusters.Add(new ObjectCluster
                    {
                        ScreenPos = screen,
                        Objects = { obj }
                    });
                }
            }
            else
            {
                // no clustering -> always its own cluster
                clusters.Add(new ObjectCluster
                {
                    ScreenPos = screen,
                    Objects = { obj }
                });
            }

        }

        // ---- Render clusters / objects ----
        foreach (var cluster in clusters)
        {
            Vector2 min;
            Vector2 max;
            bool hovered;
            bool clicked;

            var obj = cluster.Objects[0];
            bool isTradelane = IsTradeLaneRing(obj);

            // Cluster
            if (cluster.Objects.Count > 1)
            {
                bool selected = tab.ObjectsList.Selection.Any(cluster.Objects.Contains);

                float clusterIconSize = lod == MapLod.Minimal
                    ? 16f : lod == MapLod.Reduced
                    ? 32f : 64f;


                min = cluster.ScreenPos - new Vector2(clusterIconSize);
                max = cluster.ScreenPos + new Vector2(clusterIconSize);
                hovered = ImGui.IsMouseHoveringRect(min, max);
                clicked = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

                var offset = cluster.Objects.Count > 9 ? 8 : 4;

                drawList.AddCircleFilled(cluster.ScreenPos, clusterIconSize,
                    selected ? SELECTED_COLOUR : CLUSTER_BG_COLOUR);
                drawList.AddText(
                    cluster.ScreenPos - new Vector2(offset, offset),
                    0xFFFFFFFF,
                    $"{cluster.Objects.Count}"
                );

                if (clicked)
                {
                    currentClusterObjects = cluster.Objects;
                    ImGui.OpenPopup("##clusterPopup");
                }
            }
            else // Single object
            {
                bool selected = tab.ObjectsList.Selection.Contains(obj);

                var objectIconSize = 64;

                min = cluster.ScreenPos - new Vector2(objectIconSize / 2);
                max = cluster.ScreenPos + new Vector2(objectIconSize / 2);
                hovered = ImGui.IsMouseHoveringRect(min, max);
                clicked = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

                var icon = ctx.GetArchetypePreview(obj.SystemObject.Archetype);
                Vector2 imageSize = new Vector2(objectIconSize) * ImGuiHelper.Scale;

                var text = cluster.Objects[0].Nickname;
                var TextPos = new Vector2(
                    cluster.ScreenPos.X - (text.Length * 3),
                    cluster.ScreenPos.Y + imageSize.Y / 2
                );

                drawList.AddText(TextPos, COLOUR_WHITE, cluster.Objects[0].Nickname);

                drawList.AddImage(icon,min,max,
                    new Vector2(0, 1), // UV top-left
                    new Vector2(1, 0)  // UV bottom-right (flipped V)
                );

                drawList.AddRect(min,max,COLOUR_WHITE,2);

                drawList.AddRect(min, max,
                    selected ? SELECTED_COLOUR : TRADELANE_DESELECTED_COLOUR,
                    2
                );

                if (hovered)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(obj.Nickname);
                    ImGui.EndTooltip();
                }

                if (clicked)
                {
                    tab.ForceSelectObject(obj);
                }

                if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    tab.ForceSelectObject(obj);
                    dragTarget = obj;
                    dragOriginalTransform = obj.LocalTransform;
                }

                if (dragTarget == obj)
                {
                    if (draggingTradeLaneGroup != null)
                        return; // group drag takes priority

                    if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        Vector2 delta = ImGui.GetIO().MouseDelta;

                        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);
                        Vector3 worldDelta = new Vector3(
                            delta.X / mapSize * scale,
                            0,
                            delta.Y / mapSize * scale
                        );

                        obj.SetLocalTransform(
                            new Transform3D(
                                obj.LocalTransform.Position + worldDelta,
                                obj.LocalTransform.Orientation
                            )
                        );
                    }
                    if (dragTarget == obj && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        tab.UndoBuffer.Commit(
                            new ObjectSetTransform(
                                obj,
                                tab.ObjectsList,
                                dragOriginalTransform,
                                obj.LocalTransform
                            )
                        );

                        dragTarget = null;
                    }
                }
            }
        }
    }
    void DrawTradeLaneLinesLOD(SystemEditData system, SystemEditorTab tab, GameDataContext ctx, Vector2 mapTopLeft, float mapSize, ImDrawListPtr drawList, MapLod lod)
    {
        // Do not interfere while creating lanes
        if (CreationTools.Tradelane.IsActive)
            return;

        if (groupedTradelanes.Count == 0)
            return;

        var size = lod == MapLod.Minimal
                ? 2f : lod == MapLod.Reduced
                ? 4f : 6f;

        foreach (var group in groupedTradelanes)
        {
            var selected = group.Members.All(g => selectedTradeLaneGroup!= null && selectedTradeLaneGroup.Members.Contains(g));

            var (startObj, endObj) = group.GetEndpoints();
            if (startObj == null || endObj == null)
                continue;

            Vector2 start = WorldToScreen(startObj.LocalTransform.Position, system, mapTopLeft, mapSize);
            Vector2 end = WorldToScreen(endObj.LocalTransform.Position, system, mapTopLeft, mapSize);

            // draw line
            var col = selected ? SELECTED_COLOUR : TRADELANE_DESELECTED_COLOUR;
            drawList.AddLine(start, end, col, size);
        }
    }
    void DrawTradeLaneIconsLOD(SystemEditData system, SystemEditorTab tab, GameDataContext ctx, Vector2 mapTopLeft, float mapSize, ImDrawListPtr drawList, MapLod lod)
    {

        if (groupedTradelanes.Count == 0)
            return;

        var size = lod == MapLod.Minimal
            ? 12f : lod == MapLod.Reduced
            ? 20f : 96f;

        foreach (var group in groupedTradelanes)
        {
            foreach (var ring in group.Members)
            {
                bool selected = tab.ObjectsList.Selection.Contains(ring);

                Vector2 screenPos = WorldToScreen(ring.LocalTransform.Position, system, mapTopLeft, mapSize);

                Vector2 min = screenPos - new Vector2(size / 2);
                Vector2 max = screenPos + new Vector2(size / 2);
                bool hovered = ImGui.IsMouseHoveringRect(min, max);
                bool clicked = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
                bool doubleClicked = hovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

                // REDUCED: draw squares for each ring
                if (lod is MapLod.Reduced or MapLod.Minimal)
                {
                    Vector2 half = new(size * 0.5f);

                    drawList.AddRectFilled(min, max,
                        selected ? SELECTED_COLOUR : TRADELANE_DESELECTED_COLOUR);
                }
                else // draw icon
                {
                    var icon = ctx.GetArchetypePreview(ring.SystemObject.Archetype);
                    Vector2 imageSize = new Vector2(size) * ImGuiHelper.Scale;

                    drawList.AddImage(
                        icon,
                        min,
                        max
                    );

                    if (selected)
                    {
                        drawList.AddRect(
                            min,
                            max,
                            SELECTED_COLOUR,
                            2
                        );
                    }
                }

                if (hovered)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(ring.Nickname);
                    ImGui.EndTooltip();
                }



                if (clicked)
                {
                    // If this ring is part of the selected group -> start group drag
                    if (selectedTradeLaneGroup != null &&
                        selectedTradeLaneGroup.Members.Contains(ring))
                    {
                        BeginTradeLaneGroupDrag(selectedTradeLaneGroup);
                    }
                    else
                    {
                        // Normal single selection
                        selectedTradeLaneGroup = null;
                        tab.ForceSelectObject(ring);
                    }
                }
                if (doubleClicked)
                {
                    selectedTradeLaneGroup = group;
                    tab.ObjectsList.SelectMultiple(selectedTradeLaneGroup.Members);
                }
            }
        }
    }
    void DrawContextMenu(SystemEditData system, GameWorld world, GameDataContext ctx, SystemEditorTab tab, float mapSize, Vector2 mapTopLeft)
    {
        ImGui.SetNextItemAllowOverlap();
        if (ImGui.BeginPopupContextItem("##mapContext"))
        {
            var pos = ImGui.GetMousePosOnOpeningCurrentPopup();

            Vector3 worldPos = MapToWorld(
                pos - mapTopLeft,
                system,
                mapSize
            );

            if (ImGui.MenuItem("Add Object"))
            {
                tab.Popups.OpenPopup(
                    new NewObjectPopup(ctx, world, worldPos, tab.CreateObject)
                );
            }

            if (!CreationTools.Patrol.IsActive && ImGui.MenuItem("New Patrol Path"))
            {
                CreationTools.Patrol.Start();
            }

            ImGui.Separator();

            if (!CreationTools.Tradelane.IsActive && ImGui.MenuItem("New Tradelane"))
            {
                CreationTools.Tradelane.Start();
            }

            ImGui.Separator();

            if (!CreationTools.ZoneShape.IsActive && ImGui.MenuItem("New Sphere Zone"))
            {
                CreationTools.ZoneShape.Start(
                    ShapeKind.Sphere,
                    pos - mapTopLeft,
                    worldPos
                );
            }

            if (!CreationTools.ZoneShape.IsActive && ImGui.MenuItem("New Ellipsoid Zone"))
            {
                CreationTools.ZoneShape.Start(
                    ShapeKind.Ellipsoid,
                    pos - mapTopLeft,
                    worldPos
                );
            }

            ImGui.EndPopup();
        }
    }

    // coord helpers
    Vector2 WorldToScreen(Vector3 worldPos, SystemEditData system, Vector2 mapTopLeft, float mapSize)
    {
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 mapPos = new(
            (worldPos.X / scale) + 0.5f,
            (worldPos.Z / scale) + 0.5f
        );

        return mapTopLeft + mapPos * mapSize;
    }
    static Vector3 MapToWorld(Vector2 mapPos, SystemEditData system, float mapSize)
    {
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 rel = (mapPos / mapSize) - new Vector2(0.5f);
        return new Vector3(rel.X * scale, 0, rel.Y * scale);
    }
    static Vector2 WorldToMap_Local(Vector3 world, SystemEditData system, float mapSize)
    {
        // returns MAP-LOCAL coordinates (0..mapSize)
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 map01 = new(
            (world.X / scale) + 0.5f,
            (world.Z / scale) + 0.5f
        );

        return map01 * mapSize;
    }
    static Vector3 MapToWorld_Local(Vector2 mapLocal, SystemEditData system, float mapSize)
    {
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 map01 = mapLocal / mapSize - new Vector2(0.5f);

        return new Vector3(
            map01.X * scale,
            0,
            map01.Y * scale
        );
    }
    static Vector2 PolygonCentroid(ReadOnlySpan<Vector2> poly)
    {
        float area = 0f;
        float cx = 0f;
        float cy = 0f;

        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            float cross = poly[j].X * poly[i].Y - poly[i].X * poly[j].Y;
            area += cross;
            cx += (poly[j].X + poly[i].X) * cross;
            cy += (poly[j].Y + poly[i].Y) * cross;
        }

        area *= 0.5f;

        if (MathF.Abs(area) < 0.0001f)
            return poly[0]; // fallback

        float inv = 1f / (6f * area);
        return new Vector2(cx * inv, cy * inv);
    }

    static bool IsTradeLaneRing(GameObject obj)
    {
        var arch = obj.SystemObject.Archetype?.Nickname;
        return arch != null &&
               arch.Contains("trade_lane", StringComparison.OrdinalIgnoreCase)
               && obj.SystemObject.Dock.Kind == DockKinds.Tradelane;
    }
    void BeginTradeLaneGroupDrag(TradeLaneGroup group)
    {

        draggingTradeLaneGroup = group;

        tradeLaneDragStartMouse = ImGui.GetIO().MousePos;

        tradeLaneDragStartTransforms = new Dictionary<GameObject, Transform3D>();
        foreach (var obj in group.Members)
        {
            tradeLaneDragStartTransforms[obj] = obj.LocalTransform;
        }
    }
    void UpdateTradeLaneGroupDrag(SystemEditData system, float mapSize, SystemEditorTab tab)
    {
        if (draggingTradeLaneGroup == null)
            return;

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Vector2 mouseNow = ImGui.GetIO().MousePos;
            Vector2 mouseDelta = mouseNow - tradeLaneDragStartMouse;

            float scale = GridSizeDefault /
                (system.NavMapScale == 0 ? 1 : system.NavMapScale);

            Vector3 worldDelta = new(
                mouseDelta.X / mapSize * scale,
                0,
                mouseDelta.Y / mapSize * scale
            );

            foreach (var obj in draggingTradeLaneGroup.Members)
            {
                var start = tradeLaneDragStartTransforms[obj];

                obj.SetLocalTransform(
                    new Transform3D(
                        start.Position + worldDelta,
                        start.Orientation
                    )
                );
            }
        }

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            CommitTradeLaneGroupDrag(tab);
        }
    }

    void CommitTradeLaneGroupDrag(SystemEditorTab tab)
    {
        foreach (var kvp in tradeLaneDragStartTransforms)
        {
            tab.UndoBuffer.Commit(
                new ObjectSetTransform(
                    kvp.Key,
                    tab.ObjectsList,
                    kvp.Value,
                    kvp.Key.LocalTransform
                )
            );
        }

        draggingTradeLaneGroup = null;
        tradeLaneDragStartTransforms = null;
    }

    public void ClearSelectedTradelaneGroup()
    {
        if (selectedTradeLaneGroup != null) selectedTradeLaneGroup = null;
    }
}

class ObjectCluster
{
    public Vector2 ScreenPos;
    public readonly List<GameObject> Objects = new();
}
