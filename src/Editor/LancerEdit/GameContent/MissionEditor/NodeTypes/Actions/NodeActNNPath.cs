using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNNPath : BlueprintNode
{
    protected override string Name => "Set NN Path";

    public readonly Act_NNPath Data;
    public NodeActNNPath(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_NNPath(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref Data.ObjectId);
        Controls.InputTextId("System", ref Data.SystemId);
        Controls.IdsInputString("IDS 1", gameData, popup, ref Data.Ids1, (ids) => Data.Ids1 = ids);
        Controls.IdsInputString("IDS 2", gameData, popup, ref Data.Ids2, (ids) => Data.Ids2 = ids);
    }
}
