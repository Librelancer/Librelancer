using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using LibreLancer.AI;
using LibreLancer.Data.Pilots;
using LibreLancer.ImUI;

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
        graphNames = graphs.Select(x => x.Description.ToString()).ToArray();
    }
    
    public override void Draw()
    {
        ImGui.Combo("State Graph", ref selectedIndex, graphNames, graphNames.Length);
        ImGui.Columns((int)StateGraphEntry._Count + 1);
        ImGui.NextColumn();
        for (int i = 0; i < (int)StateGraphEntry._Count; i++)
        {
            ImGui.Text(((StateGraphEntry)i).ToString());
            ImGui.NextColumn();
        }
        var tab = graphs[selectedIndex];
        for (int y = 0; y < tab.Data.Count; y++)
        {
            ImGui.Text(((StateGraphEntry)y).ToString());
            ImGui.NextColumn();
            ImGui.PushFont(ImGuiHelper.SystemMonospace);
            for (int x = 0; x < (int) StateGraphEntry._Count; x++) {
                ImGui.Text(tab.Data[y][x].ToString("F2"));
                ImGui.NextColumn();
            }
            ImGui.PopFont();
        }
        ImGui.Columns(1);
    }
}