using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Audio;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPlayMusic : TriggerEntryNode
{
    protected override string Name => "Set Music";

    public readonly Act_PlayMusic Data;
    public NodeActPlayMusic(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlayMusic(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Checkbox("Reset to Default", ref Data.Reset);

        nodePopups.StringCombo("Space", Data.Space, s => Data.Space = s, gameData.MusicByName, true);
        nodePopups.StringCombo("Danger", Data.Danger, s => Data.Danger = s, gameData.MusicByName, true);
        nodePopups.StringCombo("Battle", Data.Battle, s => Data.Battle = s, gameData.MusicByName, true);
        nodePopups.StringCombo("Motif", Data.Motif, s => Data.Motif = s, gameData.MusicByName, true);

        ImGui.InputFloat("Fade", ref Data.Fade);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
