using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit;

public record struct VerticalTab(string Name, int Tag);

public class VerticalTabLayout
{
    public List<VerticalTab> TabsLeft = new List<VerticalTab>();
    public List<VerticalTab> TabsRight = new List<VerticalTab>();

    public int ActiveLeftTab = -1;
    public int ActiveRightTab = -1;
    public bool AllowClose = true;

    public delegate void TabDraw(int index);

    private TabDraw drawLeft;
    private TabDraw drawRight;
    private Action drawMiddle;

    public VerticalTabLayout(TabDraw drawLeft, TabDraw drawRight, Action drawMiddle)
    {
        this.drawLeft = drawLeft;
        this.drawRight = drawRight;
        this.drawMiddle = drawMiddle;
    }

    void DrawButtonsLeft()
    {
        if (TabsLeft.Count == 0)
            return;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4 * ImGuiHelper.Scale, 1 * ImGuiHelper.Scale));
        ImGui.BeginGroup();
        for (int i = 0; i < TabsLeft.Count; i++)
        {
            if (TabHandler.VerticalTab(TabsLeft[i].Name, ActiveLeftTab == i))
            {
                if (ActiveLeftTab == i && AllowClose)
                {
                    ActiveLeftTab = -1;
                }
                else
                {
                    ActiveLeftTab = i;
                }
            }
        }
        ImGui.EndGroup();
        ImGui.SameLine();
        ImGui.PopStyleVar();
    }

    void DrawButtonsRight()
    {
        if (TabsRight.Count == 0)
            return;
        ImGui.BeginGroup();
        for (int i = 0; i < TabsRight.Count; i++)
        {
            if (TabHandler.VerticalTab(TabsRight[i].Name, ActiveRightTab == i))
            {
                if (ActiveRightTab == i)
                {
                    ActiveRightTab = -1;
                }
                else
                {
                    ActiveRightTab = i;
                }
            }
        }
        ImGui.EndGroup();
    }

    void DrawMiddleColumn()
    {
        DrawButtonsLeft();
        if (TabsRight.Count == 0)
        {
            ImGui.BeginGroup();
            drawMiddle();
            ImGui.EndGroup();
        }
        else
        {
            var sz = ImGui.GetContentRegionAvail();
            sz.X -= ImGui.GetFrameHeightWithSpacing();
            if (sz.X < 0) sz.X = 0;
            if (sz.Y < 0) sz.Y = 0;
            ImGui.BeginChild("##middle", sz);
            drawMiddle();
            ImGui.EndChild();
            ImGui.SameLine();
            DrawButtonsRight();
        }
    }

    public void Draw()
    {
        int count = 1;
        if (ActiveLeftTab >= 0)
            count++;
        if (ActiveRightTab >= 0)
            count++;

        if (count == 1)
        {
            DrawMiddleColumn();
            return;
        }

        if (!ImGui.BeginTable($"##tabslayout{count}", count,
                ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInnerV |
                ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg))
            return;
        if (ActiveLeftTab >= 0) {
            ImGui.TableSetupColumn("##left", ImGuiTableColumnFlags.None, 0.25f);
        }
        ImGui.TableSetupColumn("##middle", ImGuiTableColumnFlags.WidthStretch);
        if (ActiveRightTab >= 0) {
            ImGui.TableSetupColumn("##right", ImGuiTableColumnFlags.None, 0.25f);
        }
        ImGui.TableNextRow();
        if (ActiveLeftTab >= 0) {
            ImGui.TableNextColumn();
            ImGui.BeginChild("##lefttab");
            drawLeft(TabsLeft[ActiveLeftTab].Tag);
            ImGui.EndChild();
        }

        ImGui.TableNextColumn();
        DrawMiddleColumn();
        if (ActiveRightTab >= 0) {
            ImGui.TableNextColumn();
            ImGui.BeginChild("##righttab");
            drawRight(TabsRight[ActiveRightTab].Tag);
            ImGui.EndChild();
        }
        ImGui.EndTable();
    }

}
