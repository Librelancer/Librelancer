using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndPlayerManeuver : BlueprintNode
{
    public enum ManeuverType
    {
        Dock,
        Formation,
        GoTo
    }

    protected override string Name => "On Player Maneuver";

    private ManeuverType type = ManeuverType.Dock;
    private string target = string.Empty;

    public NodeCndPlayerManeuver(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 1)
        {
            Enum.TryParse(entry[0].ToString()!, true, out type);
            if (entry.Count >= 2)
            {
                target = entry[1].ToString();
            }
        }

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Condition, PinKind.Input));
    }

    private readonly string[] maneuverTypes = Enum.GetNames<ManeuverType>();
    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        var index = (int)type;
        ImGui.Combo("Maneuver", ref index, maneuverTypes, maneuverTypes.Length);
        type = (ManeuverType)index;

        // TODO: transform this into a combobox of different ships or a object depending on type
        Controls.InputTextId("Target", ref target);
    }
}
