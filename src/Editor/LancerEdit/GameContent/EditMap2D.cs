using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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
    public static int MarginH = (int) (40 * ImGuiHelper.Scale);
    public static int MarginW = (int)(15 * ImGuiHelper.Scale);
    private const float GridSizeDefault = 240000;


    private static readonly string[] GRIDNUMBERS = {
        "1", "2", "3", "4", "5", "6", "7", "8"
    };

    private static readonly string[] GRIDLETTERS = {
        "A", "B", "C", "D", "E", "F", "G", "H"
    };
    private static readonly int GRID_DIVISIONS = 8;
    private static readonly float ZOOM_SPEED = 0.15f;
    private static readonly float MIN_ZOOM = 1f;
    private static readonly float MAX_ZOOM = 10f;
    private Vector2 CameraCenter = new(0.5f, 0.5f); // normalized (0..1)

    public float Zoom = 1;
    private float lastZoom = 1f;

    private GameObject dragTarget;
    private Transform3D dragOriginalTransform;
    private RenderContext renderContext;

 // Creation tools (patrols, zones)
    public Map2DCreationTools CreationTools { get; } = new();

    public void Draw(SystemEditData system, GameWorld world, GameDataContext ctx, SystemEditorTab tab, RenderContext renderContext)
    {
        DrawNew(system, world, ctx, tab, renderContext);
        return;

        var renderWidth = Math.Max(120, ImGui.GetWindowWidth() - MarginW);
        var renderHeight = Math.Max(120, ImGui.GetWindowHeight() - MarginH);

        var overlayOrigin = ImGui.GetCursorScreenPos();

        ImGui.BeginChild("##scrollchild", new Vector2(renderWidth, renderHeight), 0, ImGuiWindowFlags.HorizontalScrollbar);

        float buttonSize = (int) ((renderWidth / 838.0f) * 12f);
        if (buttonSize < 2) buttonSize = 2;

        renderWidth -= 2 * ImGuiHelper.Scale;
        renderHeight -= 2 * ImGuiHelper.Scale;
        //make it square
        renderWidth = Math.Min(renderWidth, renderHeight);
        renderHeight = renderWidth;
        renderWidth *= Zoom;
        renderHeight *= Zoom;

        ImGui.BeginChild("##edit2d", new Vector2(renderWidth, renderHeight), ImGuiChildFlags.None);

        var gridMargin = 15 * ImGuiHelper.Scale;

        var dlist = ImGui.GetWindowDrawList();
        var mapScreenPos = (Vector2)ImGui.GetWindowPos();

        var cellWidth = (renderWidth / 8f);
        var cellHeight = (renderHeight / 8f);

        ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
        for (int i = 0; i < 8; i++)
        {
            var sz = ImGui.CalcTextSize(GRIDLETTERS[i]);
            var xPos = mapScreenPos.X + i * cellWidth + (cellWidth / 2 - sz.X / 2);
            dlist.AddText(new Vector2(xPos, mapScreenPos.Y), 0xFFFFFFFF, GRIDLETTERS[i]);
            var yPos = mapScreenPos.Y + i * cellHeight + (cellHeight / 2 - sz.Y / 2);
            dlist.AddText(new Vector2(mapScreenPos.X, yPos), 0xFFFFFFFF, GRIDNUMBERS[i]);
        }
        ImGui.PopFont();
        //Draw Grid
        mapScreenPos += new Vector2(gridMargin);
        renderWidth -= 2 * gridMargin;
        renderHeight -= 2 * gridMargin;
        dlist.AddRectFilled(mapScreenPos, mapScreenPos + new Vector2(renderWidth, renderHeight), 0xFF1C0812);
        dlist.AddRect(mapScreenPos, mapScreenPos + new Vector2(renderWidth, renderHeight), 0xFFFFFFFF);
        for (int x = 1; x < 8; x++)
        {
            var pos0 = mapScreenPos + new Vector2(x * (renderWidth / 8f), 0);
            var pos1 = mapScreenPos + new Vector2(x * (renderWidth / 8f), renderHeight);
            dlist.AddLine(pos0, pos1, 0xFFFFFFFF, 1.5f);
        }
        for (int y = 1; y < 8; y++)
        {
            var pos0 = mapScreenPos + new Vector2(0, y * (renderHeight / 8f));
            var pos1 = mapScreenPos + new Vector2(renderWidth, y * (renderHeight / 8f));
            dlist.AddLine(pos0, pos1, 0xFFFFFFFF, 1.5f);
        }

        var mapScale = new Vector2(GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale));

        // Takes in Vector3 from the game world, outputs Vector2 relative to the #scrollchild window
        // drawlist calls should use windowpos + this return value
        // controls should set the cursor position to this value
        Vector2 WorldToWindow(Vector3 pos)
        {
            var relPos = (new Vector2(pos.X, pos.Z) + (mapScale / 2)) / mapScale;
            return new Vector2(gridMargin) + relPos * new Vector2(renderWidth, renderHeight);
        }

        // Takes in Vector2 relative to top left of map grid, returns Vector3 world coordinate
        // To convert to map position, subtract mapScreenPos from a screen position.
        Vector3 MapToWorld(Vector2 pos)
        {
            // Account for grid margin when converting map position to world
            var gridMargin = 15 * ImGuiHelper.Scale;
            pos -= new Vector2(gridMargin);

            var scale = new Vector3(GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale));
            scale.Y = 0;
            var relPos = (pos - new Vector2(renderWidth / 2f, renderHeight / 2f)) / new Vector2(renderWidth, renderHeight);

            return new Vector3(relPos.X, 0, relPos.Y) * scale;
        }


        int obji = 0;
        bool grabbed = false;

        GameObject dragCurrent = null;

        foreach (var obj in world.Objects)
        {
            if (obj.SystemObject == null)
                continue;
            var objPos = obj.LocalTransform.Position;
            ImGui.SetCursorPos(WorldToWindow(objPos) - new Vector2(buttonSize * 0.5f));
            var id = $"##{obj.Nickname}";

            var buttonColor = Color4.LightGray;
             if(CreationTools.IsAnyToolActive)
            {
                buttonColor.A = 0.5f;
                ImGui.BeginDisabled();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            if (ImGui.Button(id, new Vector2(buttonSize)))
            {
                tab.ForceSelectObject(obj);
            }
            ImGui.PopStyleColor();

            if(CreationTools.IsAnyToolActive)
            {
                ImGui.EndDisabled();
            }
            else
            {
                // Interaction logic only when not creating patrol
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0) && !grabbed &&
                    (dragTarget == null || dragTarget == obj))
                {
                    grabbed = true;
                    if (dragTarget == null)
                    {
                        dragTarget = obj;
                        dragOriginalTransform = obj.LocalTransform;
                    }

                    var delta = (ImGui.GetIO().MouseDelta / new Vector2(renderWidth, renderHeight)) * mapScale;
                    objPos += new Vector3(delta.X, 0, delta.Y);
                    obj.SetLocalTransform(new Transform3D(objPos, obj.LocalTransform.Orientation));
                    dragCurrent = obj;
                }

                if (ImGui.BeginItemTooltip())
                {
                    ImGui.Text(obj.Nickname);

                    var ed = obj.GetEditData(false);
                    var arch = (ed == null) ? obj.SystemObject.Archetype : ed.Archetype;
                    var star = (ed == null) ? obj.SystemObject.Star : ed.Star;
                    if (star != null)
                    {
                        ImGui.InvisibleButton("##dummy", new Vector2(80) * ImGuiHelper.Scale);
                        var min = ImGui.GetItemRectMin();
                        var max = ImGui.GetItemRectMax();
                        var r = new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
                        var dl = ImGui.GetWindowDrawList();
                        unsafe
                        {
                            dl.AddCallback((_, cmd) =>
                            {
                                renderContext.PushScissor(ImGuiHelper.GetClipRect(cmd), false);
                                tab.SunPreview.Render(star,  (Color4)(VertexDiffuse)ImGui.GetColorU32(ImGuiCol.FrameBg), renderContext, r);
                                renderContext.PopScissor();
                            }, IntPtr.Zero);
                        }
                    }
                    else if (arch != null)
                    {
                        var img = ctx.GetArchetypePreview(obj.SystemObject.Archetype);
                        ImGui.Image(img, new Vector2(80) * ImGuiHelper.Scale, new Vector2(0, 1),
                            new Vector2(1, 0));
                    }
                    ImGui.EndTooltip();
                }
            }
        }

        if (dragCurrent == null && dragTarget != null)
        {
            tab.UndoBuffer.Commit(new ObjectSetTransform(dragTarget, tab.ObjectsList, dragOriginalTransform, dragTarget.LocalTransform));
            dragTarget = null;
        }

        var windowPos = ImGui.GetWindowPos();
        foreach (var lt in tab.LightsList.Sources)
        {
            ImGui.SetCursorPos(WorldToWindow(lt.Light.Position) - new Vector2(buttonSize * 0.5f));
            var id = $"##{lt.Nickname}";
            ImGui.PushStyleColor(ImGuiCol.Button, Color4.LightYellow);
            if (ImGui.Button(id, new Vector2(buttonSize)))
            {
                tab.ForceSelectLight(lt);
            }
            ImGui.PopStyleColor();
            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text($"{Icons.Lightbulb} {lt.Nickname}");
                ImGui.EndTooltip();
            }
            if (tab.LightsList.Selected == lt)
            {
                var radius = (lt.Light.Range / mapScale.X) * renderWidth;
                dlist.AddCircle(windowPos + WorldToWindow(lt.Light.Position), radius,
                    (VertexDiffuse)Color4.Yellow);
            }
        }

        foreach (var z in tab.ZoneList.Zones)
        {
            if (!z.Visible)
                continue;
            var mesh = z.Current.TopDownMesh();
            var transformed = ArrayPool<Vector2>.Shared.Rent(mesh.Length);
            for (int i = 0; i < mesh.Length; i++)
                transformed[i] = windowPos + WorldToWindow(z.Current.Position + new Vector3(mesh[i].X, 0, mesh[i].Y));
            dlist.AddTriangleMesh(transformed, mesh.Length, (VertexDiffuse)Color4.Pink.ChangeAlpha(0.12f));
            mesh = z.Current.OutlineMesh();
            for (int i = 0; i < mesh.Length; i++)
                transformed[i] = windowPos + WorldToWindow(z.Current.Position + new Vector3(mesh[i].X, 0, mesh[i].Y));
            dlist.AddPolyline(ref transformed[0], mesh.Length, (VertexDiffuse)Color4.Red, ImDrawFlags.None, 2f);
            ArrayPool<Vector2>.Shared.Return(transformed);
        }

        //Context menu
        ImGui.SetCursorPos(new Vector2(gridMargin));
        ImGui.InvisibleButton("##canvas", new Vector2(renderWidth, renderHeight));

        if (ImGui.BeginPopupContextItem())
        {
            var pos = ImGui.GetMousePosOnOpeningCurrentPopup();
            if (ImGui.MenuItem("Add Object"))
            {
                FLLog.Info("Obj", $"Add at {pos - mapScreenPos}");
                tab.Popups.OpenPopup(new NewObjectPopup(ctx, world, MapToWorld(pos - mapScreenPos), tab.CreateObject));
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
                CreationTools.ZoneShape.Start(ShapeKind.Sphere, pos - windowPos, MapToWorld(pos - windowPos));
            }
            if (!CreationTools.ZoneShape.IsActive && ImGui.MenuItem("New Ellipsoid Zone"))
            {
                CreationTools.ZoneShape.Start(ShapeKind.Ellipsoid, pos - windowPos, MapToWorld(pos - windowPos));
            }
            ImGui.EndPopup();
        }


        // Draw creation tools (patrol and zones)
        var helpText = CreationTools.Draw(dlist, windowPos, renderWidth, WorldToWindow, MapToWorld, tab);

        ImGui.EndChild();
        ImGui.EndChild();

        // Help text popup must live outside of scrolling region
        if (helpText != null)
        {
            var dim = ImGui.CalcTextSize(helpText);
            var pad = 4 * ImGuiHelper.Scale;
            var yHeight = overlayOrigin.Y + gridMargin;
            var w = ImGui.GetContentRegionAvail().X;
            ImGui.SetNextWindowPos(new (w - dim.X - 3 * pad, yHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSize(dim + new Vector2(4 * pad), ImGuiCond.Always);
            if (ImGui.Begin("##helpText", ImGuiWindowFlags.NoInputs |
                                          ImGuiWindowFlags.NoDecoration))
            {
                ImGui.Text(helpText);
            }
            ImGui.End();
        }
    }

    private void DrawNew(
    SystemEditData system,
    GameWorld world,
    GameDataContext ctx,
    SystemEditorTab tab,
    RenderContext renderContext
)
    {
        Vector2 viewportPos = ImGui.GetCursorScreenPos();
        Vector2 viewportSize = new(
            Math.Max(120, ImGui.GetWindowWidth() - MarginW),
            Math.Max(120, ImGui.GetWindowHeight() - MarginH)
        );
        Vector2 viewportMax = viewportPos + viewportSize;

        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(viewportPos, viewportMax, true);

        Vector2 viewportCenter = viewportPos + viewportSize * 0.5f;

        if (Zoom <= 1.0f)
        {
            Zoom = 1.0f;
            CameraCenter = new Vector2(0.5f, 0.5f);
        }

        float baseMapSize = Math.Min(viewportSize.X, viewportSize.Y);
        float mapSize = baseMapSize * Zoom;

        float halfVisible = 0.5f / Zoom;
        CameraCenter.X = Math.Clamp(CameraCenter.X, halfVisible, 1f - halfVisible);
        CameraCenter.Y = Math.Clamp(CameraCenter.Y, halfVisible, 1f - halfVisible);

        Vector2 mapTopLeft =
            viewportCenter
            - (CameraCenter * mapSize);

        // ---- Background ----
        drawList.AddRectFilled(
            mapTopLeft,
            mapTopLeft + new Vector2(mapSize),
            0xFF1C0812
        );

        drawList.AddRect(
            mapTopLeft,
            mapTopLeft + new Vector2(mapSize),
            0xFFFFFFFF,
            0,
            ImDrawFlags.None,
            2f
        );

        // ---- Grid + Labels (NEW) ----
        DrawGridAndLabelsViewportAware(
            drawList,
            viewportPos,
            viewportSize,
            mapTopLeft,
            mapSize
        );

        // ---- Mouse pan ----
        if (ImGui.IsWindowHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
        {
            Vector2 delta = ImGui.GetIO().MouseDelta;
            CameraCenter -= delta / mapSize;
        }
        // ---- Mouse Scroll ----
        else if(ImGui.IsWindowHovered() && ImGui.GetIO().MouseWheel != 0f)
        {
            float wheel = ImGui.GetIO().MouseWheel;

            float oldZoom = Zoom;

            // Exponential zoom feels better than linear
            Zoom *= MathF.Exp(wheel * ZOOM_SPEED);
            Zoom = Math.Clamp(Zoom, MIN_ZOOM, MAX_ZOOM);

            if (Zoom != oldZoom)
            {
                // Mouse position in screen space
                Vector2 mouseScreen = ImGui.GetIO().MousePos;

                // Convert mouse position to normalized map space BEFORE zoom
                Vector2 mouseMapBefore =
                    (mouseScreen - mapTopLeft) / mapSize;

                // Recalculate map size AFTER zoom
                float newMapSize = baseMapSize * Zoom;

                // Update camera center so mouseMapBefore stays under cursor
                Vector2 mouseMapAfter =
                    (mouseScreen - viewportCenter) / newMapSize + CameraCenter;

                CameraCenter += (mouseMapBefore - mouseMapAfter);
            }
        }



        drawList.PopClipRect();
    }



    private void DrawGridAndLabelsViewportAware(
    ImDrawListPtr drawList,
    Vector2 viewportPos,
    Vector2 viewportSize,
    Vector2 mapTopLeft,
    float mapSize
)
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



}


