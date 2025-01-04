using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;
public class NodeCndNpcSystemEnter : BlueprintNode
{
    protected override string Name => "On NPC System Enter";

    private string system = string.Empty;
    private List<string> ships = [];

    public NodeCndNpcSystemEnter(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        foreach (var value in entry)
        {
            if (system == string.Empty)
            {
                system = value.ToString()!;
            }
            else
            {
                ships.Add(value.ToString()!);
            }
        }

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("System", ref system);
        Controls.InputStringList("Ships", ships);
    }
}
