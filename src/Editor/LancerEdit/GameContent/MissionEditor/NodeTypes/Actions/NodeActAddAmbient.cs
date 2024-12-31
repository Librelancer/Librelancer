using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAddAmbient : BlueprintNode
{
    protected override string Name => "Add Ambient";

    private readonly Act_AddAmbient data;
    public NodeActAddAmbient(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_AddAmbient(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Script", ref data.Script);
        Controls.InputTextId("Base", ref data.Base);
        Controls.InputTextId("Room", ref data.Room);
    }
}
