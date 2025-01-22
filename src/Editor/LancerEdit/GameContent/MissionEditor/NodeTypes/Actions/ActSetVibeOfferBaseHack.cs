using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetVibeOfferBaseHack : NodeTriggerEntry
{
    public override string Name => "Set Base To Friendly";

    public readonly Act_SetVibeOfferBaseHack Data;
    public ActSetVibeOfferBaseHack(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeOfferBaseHack(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        // TODO: Transform into combo if possible
        Controls.InputTextId("Base", ref Data.Id);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
