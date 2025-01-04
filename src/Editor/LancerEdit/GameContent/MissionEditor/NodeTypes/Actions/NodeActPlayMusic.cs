using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPlayMusic : BlueprintNode
{
    protected override string Name => "Play Music";

    private readonly Act_PlayMusic data;
    public NodeActPlayMusic(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_PlayMusic(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
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
