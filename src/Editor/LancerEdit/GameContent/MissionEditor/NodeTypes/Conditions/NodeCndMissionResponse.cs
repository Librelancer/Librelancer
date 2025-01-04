using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndMissionResponse : BlueprintNode
{
    protected override string Name => "On Mission Response";

    private bool accept;
    public NodeCndMissionResponse(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 1)
        {
            accept = entry[0].ToString()!.Equals("accept", StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(id++, this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Accept", ref accept);
    }
}
