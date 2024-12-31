using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPlayMusic : BlueprintNode
{
    protected override string Name => "Play Music";

    private readonly Act_PlayMusic data;
    public NodeActPlayMusic(ref int id, Act_PlayMusic data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Music Id", ref data.Music);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            data.Music = "none";
        }

        ImGui.InputFloat("Fade", ref data.Fade);
    }
}
