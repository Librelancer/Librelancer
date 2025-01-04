using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSave : BlueprintNode
{
    protected override string Name => "Save Game";

    private readonly Act_Save data;
    public NodeActSave(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_Save(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));

        Outputs.Add(new NodePin(id++, this, LinkType.Trigger, PinKind.Output));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.IdsInputString("IDS", gameData, popup, ref data.Ids, (ids) => data.Ids = ids);
    }
}
