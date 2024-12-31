using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActCloak : BlueprintNode
{
    protected override string Name => "Act Cloak";

    public readonly Act_Cloak Data;
    public NodeActCloak(ref int id, Act_Cloak data) : base(ref id, NodeColours.Action)
    {
        Data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Target", ref Data.Target);
        ImGui.Checkbox("Cloak", ref Data.Cloaked);
    }
}
