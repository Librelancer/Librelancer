using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNNPath : BlueprintNode
{
    protected override string Name => "Set NN Path";

    private readonly Act_NNPath data;
    public NodeActNNPath(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_NNPath(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref data.ObjectId);
        Controls.InputTextId("System", ref data.SystemId);
        Controls.IdsInputString("IDS 1", gameData, popup, ref data.Ids1, (ids) => data.Ids1 = ids);
        Controls.IdsInputString("IDS 2", gameData, popup, ref data.Ids2, (ids) => data.Ids2 = ids);
    }
}
