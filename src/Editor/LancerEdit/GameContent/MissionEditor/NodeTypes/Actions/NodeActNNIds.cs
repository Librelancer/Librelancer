using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNNIds : BlueprintNode
{
    protected override string Name => "Add IDS To NN Log";

    private readonly Act_NNIds data;
    public NodeActNNIds(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_NNIds(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.IdsInputString("IDS", gameData, popup, ref data.Ids, (ids) => data.Ids = ids);
    }
}
