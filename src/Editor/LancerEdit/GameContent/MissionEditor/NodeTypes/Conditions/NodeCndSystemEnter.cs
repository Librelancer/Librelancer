using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndSystemEnter : BlueprintNode
{
    protected override string Name => "On System Enter";

    private List<string> systems = [];
    public bool any;

    public NodeCndSystemEnter(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        foreach (var system in entry)
        {
            if (system.ToString()!.Equals("any", StringComparison.InvariantCultureIgnoreCase))
            {
                systems = [];
                any = true;
                break;
            }

            systems.Add(system.ToString()!);
        }

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Any", ref any);
        ImGui.BeginDisabled(any);
        Controls.InputStringList("Systems", systems);
        ImGui.EndDisabled();
    }
}
