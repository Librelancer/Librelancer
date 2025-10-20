using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit;

public record struct VerticalTab(char Icon, string Name, int Tag);

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

    void DrawButtonsLeft(VerticalTabStyle mode)
    {
        if (TabsLeft.Count == 0)
            return;
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4 * ImGuiHelper.Scale, 1 * ImGuiHelper.Scale));
        ImGui.BeginGroup();
        for (int i = 0; i < TabsLeft.Count; i++)
        {
            if (TabHandler.VerticalTab(TabsLeft[i].Icon, TabsLeft[i].Name, mode, ActiveLeftTab == i))
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

    void DrawButtonsRight(VerticalTabStyle mode)
    {
        if (TabsRight.Count == 0)
            return;
        ImGui.BeginGroup();
        for (int i = 0; i < TabsRight.Count; i++)
        {
            if (TabHandler.VerticalTab(TabsRight[i].Icon, TabsRight[i].Name, mode, ActiveRightTab == i))
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

    void DrawMiddleColumn(VerticalTabStyle mode)
    {
        DrawButtonsLeft(mode);
        if (TabsRight.Count == 0)
        {
            ImGui.BeginGroup();
            drawMiddle();
            ImGui.EndGroup();
        }
        else
        {
            var sz = ImGui.GetContentRegionAvail();
            sz.X -= (ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.X);
            if (sz.X < 0) sz.X = 0;
            if (sz.Y < 0) sz.Y = 0;
            ImGui.BeginChild("##middle", sz);
            drawMiddle();
            ImGui.EndChild();
            ImGui.SameLine();
            DrawButtonsRight(mode);
        }
    }

    public void Draw(VerticalTabStyle mode)
    {
        int count = 1;
        if (ActiveLeftTab >= 0)
            count++;
        if (ActiveRightTab >= 0)
            count++;

        if (count == 1)
        {
            DrawMiddleColumn(mode);
            return;
        }

        if (!ImGui.BeginTable($"##tabslayout{count}", count,
                ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.BordersInnerV |
                ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg))
            return;
        if (ActiveLeftTab >= 0) {
            ImGui.TableSetupColumn("##left", ImGuiTableColumnFlags.WidthStretch, 0.25f);
        }
        ImGui.TableSetupColumn("##middle", ImGuiTableColumnFlags.WidthStretch);
        if (ActiveRightTab >= 0) {
            ImGui.TableSetupColumn("##right", ImGuiTableColumnFlags.WidthStretch, 0.25f);
        }
        ImGui.TableNextRow();
        if (ActiveLeftTab >= 0) {
            ImGui.TableNextColumn();
            ImGui.BeginChild("##lefttab");
            drawLeft(TabsLeft[ActiveLeftTab].Tag);
            ImGui.EndChild();
        }

        ImGui.TableNextColumn();
        DrawMiddleColumn(mode);
        if (ActiveRightTab >= 0) {
            ImGui.TableNextColumn();
            ImGui.BeginChild("##righttab");
            drawRight(TabsRight[ActiveRightTab].Tag);
            ImGui.EndChild();
        }
        ImGui.EndTable();
    }

}
