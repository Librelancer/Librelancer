using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetOffer : NodeTriggerEntry
{
    public override string Name => "Set Mission Offer";

    public readonly Act_SetOffer Data;
    public ActSetOffer(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetOffer(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.IdsInputString("IDS", gameData, popup, ref Data.Ids, (ids) => Data.Ids = ids);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
