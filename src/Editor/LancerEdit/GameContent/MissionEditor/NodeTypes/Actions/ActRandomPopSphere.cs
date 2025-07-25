using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActRandomPopSphere : NodeTriggerEntry
{
    public override string Name => "Toggle Random Population Sphere";

    public readonly Act_RandomPopSphere Data;
    public ActRandomPopSphere(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_RandomPopSphere(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.InputFloat3("Position", ref Data.Position);
        ImGui.InputFloat("Radius", ref Data.Radius);
        ImGui.Checkbox("Enable", ref Data.On);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
