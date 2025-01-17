using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Audio;
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
        var music = gameData.GameData.AllSounds.Where(x => x.Type == AudioType.Music).Select(x => x.Nickname).ToArray();
        nodePopups.StringCombo("Music", Data.Music, s => Data.Music = s, music);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            Data.Music = "none";
        }

        ImGui.InputFloat("Fade", ref Data.Fade);
    }
}
