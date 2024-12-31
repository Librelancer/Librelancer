using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAddAmbient : BlueprintNode
{
    protected override string Name => "Add Ambient";

    private readonly Act_AddAmbient data;
    public NodeActAddAmbient(ref int id, Act_AddAmbient data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Script", ref data.Script);
        Controls.InputTextId("Base", ref data.Base);
        Controls.InputTextId("Room", ref data.Room);
    }
}
