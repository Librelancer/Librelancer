using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActLightFuse : NodeTriggerEntry
{
    public override string Name => "Light Fuse";

    public readonly Act_LightFuse Data;
    public ActLightFuse(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_LightFuse(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Fuse", ref Data.Fuse);
        Controls.InputTextId("Target", ref Data.Target);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
