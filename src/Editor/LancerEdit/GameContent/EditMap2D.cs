using System;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Popups;
using LibreLancer;
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

    public float Zoom = 1;

    private GameObject dragTarget;
    private Transform3D dragOriginalTransform;
    public void Draw(SystemEditData system, GameWorld world, GameDataContext ctx, SystemEditorTab tab)
    {
        var renderWidth = Math.Max(120, ImGui.GetWindowWidth() - MarginW);
        var renderHeight = Math.Max(120, ImGui.GetWindowHeight() - MarginH);

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
        var wPos = (Vector2)ImGui.GetWindowPos();

        var cellWidth = (renderWidth / 8f);
        var cellHeight = (renderHeight / 8f);

        ImGui.PushFont(ImGuiHelper.SystemMonospace);
        for (int i = 0; i < 8; i++)
        {
            var sz = ImGui.CalcTextSize(GRIDLETTERS[i]);
            var xPos = wPos.X + i * cellWidth + (cellWidth / 2 - sz.X / 2);
            dlist.AddText(new Vector2(xPos, wPos.Y), 0xFFFFFFFF, GRIDLETTERS[i]);
            var yPos = wPos.Y + i * cellHeight + (cellHeight / 2 - sz.Y / 2);
            dlist.AddText(new Vector2(wPos.X, yPos), 0xFFFFFFFF, GRIDNUMBERS[i]);
        }
        ImGui.PopFont();
        //Draw Grid
        wPos += new Vector2(gridMargin);
        renderWidth -= 2 * gridMargin;
        renderHeight -= 2 * gridMargin;
        dlist.AddRectFilled(wPos, wPos + new Vector2(renderWidth, renderHeight), 0xFF1C0812);
        dlist.AddRect(wPos, wPos + new Vector2(renderWidth, renderHeight), 0xFFFFFFFF);
        for (int x = 1; x < 8; x++)
        {
            var pos0 = wPos + new Vector2(x * (renderWidth / 8f), 0);
            var pos1 = wPos + new Vector2(x * (renderWidth / 8f), renderHeight);
            dlist.AddLine(pos0, pos1, 0xFFFFFFFF, 1.5f);
        }
        for (int y = 1; y < 8; y++)
        {
            var pos0 = wPos + new Vector2(0, y * (renderHeight / 8f));
            var pos1 = wPos + new Vector2(renderWidth, y * (renderHeight / 8f));
            dlist.AddLine(pos0, pos1, 0xFFFFFFFF, 1.5f);
        }

        var mapScale = new Vector2(GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale));

        Vector2 WorldToMap(Vector3 pos)
        {
            var relPos = (new Vector2(pos.X, pos.Z) + (mapScale / 2)) / mapScale;
            return new Vector2(gridMargin) + relPos * new Vector2(renderWidth, renderHeight);
        }

        int obji = 0;
        bool grabbed = false;

        GameObject dragCurrent = null;

        foreach (var obj in world.Objects)
        {
            if (obj.SystemObject == null)
                continue;
            var objPos = obj.LocalTransform.Position;
            ImGui.SetCursorPos(WorldToMap(objPos) - new Vector2(buttonSize * 0.5f));
            var id = $"##{obj.Nickname}";
            ImGui.PushStyleColor(ImGuiCol.Button, Color4.LightGray);
            ImGui.Button(id, new Vector2(buttonSize));
            ImGui.PopStyleColor();
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0) && !grabbed &&
                (dragTarget == null || dragTarget == obj))
            {
                grabbed = true;
                if (dragTarget == null) {
                    dragTarget = obj;
                    dragOriginalTransform = obj.LocalTransform;
                }
                var delta = (ImGui.GetIO().MouseDelta / new Vector2(renderWidth, renderHeight)) * mapScale;
                Console.WriteLine($"dragging {delta}");
                objPos += new Vector3(delta.X, 0, delta.Y);
                obj.SetLocalTransform(new Transform3D(objPos, obj.LocalTransform.Orientation));
                dragCurrent = obj;
            }

            if (ImGui.BeginItemTooltip())
            {
                ImGui.TextUnformatted(obj.Nickname);
                if (obj.SystemObject?.Archetype != null)
                {
                    var img = ctx.GetArchetypePreview(obj.SystemObject.Archetype);
                    ImGui.Image((IntPtr)img, new Vector2(80) * ImGuiHelper.Scale, new Vector2(0, 1), new Vector2(1, 0));
                }

                ImGui.EndTooltip();
            }
        }

        if (dragCurrent == null && dragTarget != null)
        {
            tab.UndoBuffer.Commit(new ObjectSetTransform(dragTarget, dragOriginalTransform, dragTarget.LocalTransform, tab.ObjectsList));
            dragTarget = null;
        }

        foreach (var lt in tab.LightsList.Sources)
        {
            ImGui.SetCursorPos(WorldToMap(lt.Light.Position) - new Vector2(buttonSize * 0.5f));
            var id = $"##{lt.Nickname}";
            ImGui.PushStyleColor(ImGuiCol.Button, Color4.LightYellow);
            ImGui.Button(id, new Vector2(buttonSize));
            ImGui.PopStyleColor();
            if (tab.LightsList.Selected == lt)
            {
                var radius = (lt.Light.Range / mapScale.X) * renderWidth;
                dlist.AddCircle(ImGui.GetWindowPos() + WorldToMap(lt.Light.Position), radius,
                    (VertexDiffuse)Color4.Yellow);
            }
        }
        //Context menu
        ImGui.SetCursorPos(new Vector2(gridMargin));
        ImGui.InvisibleButton("##canvas", new Vector2(renderWidth, renderHeight));

        Vector3 MapToWorld(Vector2 pos)
        {
            var scale = new Vector3(GridSizeDefault / (system.NavMapScale == 0 ? 1 : system.NavMapScale));
            scale.Y = 0;
            var relPos = (pos - new Vector2(renderWidth / 2f, renderHeight / 2f)) /
                         new Vector2(renderWidth, renderHeight);
            return new Vector3(relPos.X, 0, relPos.Y) * scale;
        }

        if (ImGui.BeginPopupContextItem())
        {
            var pos = ImGui.GetMousePosOnOpeningCurrentPopup();
            if (ImGui.MenuItem("Add Object"))
            {
                FLLog.Info("Obj", $"Add at {pos - wPos}");
                tab.Popups.OpenPopup(new NewObjectPopup(ctx, world, MapToWorld(pos - wPos), tab.CreateObject));
            }
            ImGui.EndPopup();
        }

        ImGui.EndChild();
        ImGui.EndChild();
    }



}
