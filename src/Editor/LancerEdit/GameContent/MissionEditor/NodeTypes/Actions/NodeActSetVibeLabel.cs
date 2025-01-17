using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeLabel : BlueprintNode
{
    protected override string Name => "Set Vibe Label";

    public readonly Act_SetVibeLbl Data;

    public NodeActSetVibeLabel(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeLbl(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        NodeActSetVibe.VibeComboBox(ref Data.Vibe, nodePopups);
        Controls.InputTextId("Label 1", ref Data.Label1);
        Controls.InputTextId("Label 2", ref Data.Label2);
    }
}
