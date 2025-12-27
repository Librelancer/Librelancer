using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.Data.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
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


    private static readonly float ZOOM_SPEED = 0.15f;
    private static readonly float MIN_ZOOM = 1f;
    private static readonly float MAX_ZOOM = 10f;
    private Vector2 CameraCenter = new(0.5f, 0.5f); // normalized (0..1)

    public float Zoom = 1;
    private float lastZoom = 1f;

    private enum MapLod { Minimal, Reduced, Detailed }

    GameObject dragTarget;
    Transform3D dragOriginalTransform;
    RenderContext renderContext;
    List<GameObject> currentClusterObjects;

    // Creation tools (patrols, zones)
    public Map2DCreationTools CreationTools { get; } = new();

    public void Draw(SystemEditData system, GameWorld world, GameDataContext ctx, SystemEditorTab tab, RenderContext renderContext)
    {
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
        drawList.AddRectFilled(mapTopLeft, mapTopLeft + new Vector2(mapSize), 0xFF1C0812);

        drawList.AddRect(mapTopLeft, mapTopLeft + new Vector2(mapSize), 0xFFFFFFFF, 0, ImDrawFlags.None, 2f);

        // Grid + labels
        DrawGridAndLabelsViewportAware(drawList, canvasMin, canvasSize, mapTopLeft, mapSize);

        // LOD
        MapLod lod =
            Zoom < 2f ? MapLod.Minimal :
            Zoom < 5f ? MapLod.Reduced :
                        MapLod.Detailed;

        DrawTradeLanesLOD(system, tab, mapTopLeft, mapSize, drawList, lod);
        DrawObjectsLOD(system, tab, ctx, mapTopLeft, mapSize, drawList, lod);

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

        // Creation tools (correct coordinate contract)
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

    static bool IsTradeLaneRing(GameObject obj)
    {
        return obj.SystemObject?.Archetype?.Nickname == "Trade_Lane_Ring";
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
    void DrawObjectsLOD(SystemEditData system, SystemEditorTab tab, GameDataContext ctx, Vector2 mapTopLeft, float mapSize, ImDrawListPtr drawList, MapLod lod)
    {
        float clusterRadius =
            lod == MapLod.Minimal ? 60f :
            lod == MapLod.Reduced ? 20f :
                                    30f;

        List<ObjectCluster> clusters = new();

        // ---- Build clusters ----
        foreach (var obj in tab.ObjectsList.Objects)
        {
            bool isTradelane = IsTradeLaneRing(obj);

            Vector2 screen = WorldToScreen(
                obj.LocalTransform.Position,
                system,
                mapTopLeft,
                mapSize
            );

            // Tradelanes NEVER cluster
            if (clusterRadius > 0 && !isTradelane)
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
                // Tradelane OR no clustering -> always its own cluster
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

            var obj = cluster.Objects[0];
            bool isTradelane = IsTradeLaneRing(obj);

            Vector2 min = cluster.ScreenPos - new Vector2(12);
            Vector2 max = cluster.ScreenPos + new Vector2(12);

            bool hovered = ImGui.IsMouseHoveringRect(min, max);
            bool clicked = hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

            float size = 0;
            if (isTradelane)
            {
                size =
                        lod == MapLod.Minimal ? 4f :
                        lod == MapLod.Reduced ? 8f :
                                                12f;
            }
            else
            {
                size =
                        lod == MapLod.Minimal ? 16f :
                        lod == MapLod.Reduced ? 32f :
                                                100f;
            }

            // Cluster icon (zoom 1->7)
            if (cluster.Objects.Count > 1 && lod != MapLod.Detailed)
            {
                drawList.AddCircleFilled(cluster.ScreenPos, size, 0xFF8888FF);
                drawList.AddText(
                    cluster.ScreenPos - new Vector2(8, 8),
                    0xFFFFFFFF,
                    $"{cluster.Objects.Count}" 
                );

                //if (hovered)
                //{
                //    ImGui.BeginTooltip();
                //    foreach (var o in cluster.Objects)
                //        ImGui.Text(o.Nickname);
                //    ImGui.EndTooltip();
                //}

                if (clicked)
                {
                    currentClusterObjects = cluster.Objects;
                    ImGui.OpenPopup("##clusterPopup");
                }
            }
            // Single object
            else
            {
                bool selected = tab.ObjectsList.Selection.Contains(obj);

                

                if (lod == MapLod.Detailed)
                {
                    // Icon instead of circle
                    var icon = ctx.GetArchetypePreview(obj.SystemObject.Archetype);
                    Vector2 imageSize = new Vector2(96, 96) * ImGuiHelper.Scale;

                    drawList.AddImage(
                        icon,
                        cluster.ScreenPos - imageSize * 0.5f,
                        cluster.ScreenPos + imageSize * 0.5f
                    );

                    if (selected)
                    {
                        drawList.AddCircle(
                            cluster.ScreenPos,
                            imageSize.X * 0.6f,
                            0xFFFFFF00,
                            0,
                            2f
                        );
                    }
                }
                else if (lod == MapLod.Reduced)
                {
                    // Icon instead of circle
                    var icon = ctx.GetArchetypePreview(obj.SystemObject.Archetype);
                    Vector2 imageSize = new Vector2(64, 64) * ImGuiHelper.Scale;

                    drawList.AddImage(
                        icon,
                        cluster.ScreenPos - imageSize * 0.5f,
                        cluster.ScreenPos + imageSize * 0.5f
                    );

                    if (selected)
                    {
                        drawList.AddCircle(
                            cluster.ScreenPos,
                            imageSize.X * 0.6f,
                            0xFFFFFF00,
                            0,
                            2f
                        );
                    }
                } else
                {
                    if (isTradelane)
                    {
                        uint lightBlue = ImGui.ColorConvertFloat4ToU32(
                            new Vector4(
                                0.4f,  // R
                                0.7f,  // G
                                1.0f,  // B
                                1.0f   // A
                            )
                        );
                        drawList.AddCircleFilled(cluster.ScreenPos, size, lightBlue);
                        if (selected)
                        {
                            Vector2 selectedSize = new Vector2(size + 5);
                            drawList.AddRect(
                                selectedSize,
                                selectedSize,
                                0xFF4DA6FF,
                                2f,
                                ImDrawFlags.None,
                                2f
                            );
                        }
                    }
                    else
                    {
                        size =
                        lod == MapLod.Minimal ? 16f :
                        lod == MapLod.Reduced ? 64f :
                                                128f;
                        Vector2 half = new Vector2(size * 0.5f);
                        Vector2 minSize = cluster.ScreenPos - half;
                        Vector2 maxSize = cluster.ScreenPos + half;

                        // Filled square
                        drawList.AddRectFilled(minSize, maxSize, 0xFFFFFFFF);
                        // Blue selection outline
                        if (selected)
                        {
                            drawList.AddRect(
                                minSize,
                                maxSize,
                                0xFF4DA6FF,
                                0f,
                                ImDrawFlags.None,
                                2f
                            );
                        }
                    }
                }

                if (hovered && lod != MapLod.Minimal)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(obj.Nickname);
                    ImGui.EndTooltip();
                }

                if (clicked)
                {
                    tab.ForceSelectObject(obj);
                }

                Vector2 mouse = ImGui.GetIO().MousePos;

                if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    tab.ForceSelectObject(obj);
                    dragTarget = obj;
                    dragOriginalTransform = obj.LocalTransform;
                }

                if (dragTarget == obj &&
                    ImGui.IsMouseDragging(ImGuiMouseButton.Left))
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

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                    dragTarget == obj)
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
    void DrawTradeLanesLOD(SystemEditData system, SystemEditorTab tab, Vector2 mapTopLeft, float mapSize, ImDrawListPtr drawList, MapLod lod)
    {
        // Intentionally empty.
        // Tradelanes are currently rendered as individual rings only.
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


    Vector2 WorldToScreen(Vector3 worldPos, SystemEditData system, Vector2 mapTopLeft, float mapSize)
    {
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 mapPos = new(
            (worldPos.X / scale) + 0.5f,
            (worldPos.Z / scale) + 0.5f
        );

        return mapTopLeft + mapPos * mapSize;
    }
    Vector3 MapToWorld(Vector2 mapPos, SystemEditData system, float mapSize)
    {
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 rel = (mapPos / mapSize) - new Vector2(0.5f);
        return new Vector3(rel.X * scale, 0, rel.Y * scale);
    }
    Vector2 WorldToMap_Local(Vector3 world, SystemEditData system, float mapSize)
    {
        // returns MAP-LOCAL coordinates (0..mapSize)
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 map01 = new(
            (world.X / scale) + 0.5f,
            (world.Z / scale) + 0.5f
        );

        return map01 * mapSize;
    }
    Vector3 MapToWorld_Local(Vector2 mapLocal, SystemEditData system, float mapSize)
    {
        float scale = GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale);

        Vector2 map01 = mapLocal / mapSize - new Vector2(0.5f);

        return new Vector3(
            map01.X * scale,
            0,
            map01.Y * scale
        );
    }
}

class ObjectCluster
{
    public Vector2 ScreenPos;
    public readonly List<GameObject> Objects = new();
}
