using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAddAmbient : BlueprintNode
{
    protected override string Name => "Add Ambient";

    public readonly Act_AddAmbient Data;
    public NodeActAddAmbient(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_AddAmbient(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Script", ref Data.Script);
        Controls.InputTextId("Base", ref Data.Base);
        Controls.InputTextId("Room", ref Data.Room);
    }
}
