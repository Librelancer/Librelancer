using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActInvulnerable : BlueprintNode
{
    protected override string Name => "Set Invulnerable";

    public readonly Act_Invulnerable Data;
    public NodeActInvulnerable(ref int id, Act_Invulnerable data) : base(ref id, NodeColours.Action)
    {
        Data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        ImGui.Checkbox("Is Invulnerable", ref Data.Invulnerable);
    }
}
