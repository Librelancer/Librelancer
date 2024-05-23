using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Pilots;
using LibreLancer.ImUI;
using LibreLancer.Server.Ai;
using SimpleMesh;

namespace LancerEdit;

public class StateGraphTab : EditorTab
{
    private List<StateGraph> graphs;
    private string[] graphNames;
    private int selectedIndex = 0;

    public StateGraphTab(StateGraphDb stateGraphDb, string filename)
    {
        Title = filename;
        graphs = stateGraphDb.Tables.Values.ToList();
        graphNames = graphs.Select(x => $"{x.Description.Name} ({x.Description.Type})").ToArray();

    }

    private int lastHoveredX = -1;
    private int lastHoveredY = -1;
    public override void Draw(double elapsed)
    {
        ImGui.Combo("State Graph", ref selectedIndex, graphNames, graphNames.Length);
        var tab = graphs[selectedIndex];
        int hoveredX = -1, hoveredY = -1;
        if (ImGui.BeginTable("stategraphTable", (int) StateGraphEntry._Count + 1,
                ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableHeadersRow();
            for (int i = 0; i < (int)StateGraphEntry._Count; i++)
            {
                ImGui.TableSetColumnIndex(i + 1);
                if(lastHoveredX == i)
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (VertexDiffuse)Color4.CornflowerBlue);
                ImGui.Text(((StateGraphEntry)i).ToString());
            }
            for (int y = 0; y < tab.Data.Count; y++)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TableHeader(((StateGraphEntry)y).ToString());
                if(lastHoveredY == y)
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (VertexDiffuse)Color4.CornflowerBlue);
                ImGui.PushFont(ImGuiHelper.SystemMonospace);
                for (int x = 0; x < (int) StateGraphEntry._Count; x++)
                {
                    ImGui.TableSetColumnIndex(x + 1);
                    ImGui.Selectable(tab.Data[y][x].ToString("F2"));
                    if (ImGui.IsItemHovered()) {
                        hoveredX = x;
                        hoveredY = y;
                    }
                }
                ImGui.PopFont();
            }
            ImGui.EndTable();
        }
        lastHoveredX = hoveredX;
        lastHoveredY = hoveredY;
    }
}
