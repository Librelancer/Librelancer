using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPlayMusic : BlueprintNode
{
    protected override string Name => "Play Music";

    public readonly Act_PlayMusic Data;
    public NodeActPlayMusic(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlayMusic(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Music Id", ref Data.Music);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            Data.Music = "none";
        }

        ImGui.InputFloat("Fade", ref Data.Fade);
    }
}
