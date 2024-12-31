using System;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using static System.Enum;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

// i am not convinced that watch vibe works in the base game, needs testing

public class NodeCndWatchVibe : BlueprintNode
{
    protected override string Name => "On Vibe State Change (Reputation)";

    // How does the source feel about the target

    private VibeSet vibe = VibeSet.REP_NEUTRAL;
    private string sourceObject = string.Empty;
    private string targetObject = string.Empty;
    private int modifierIndex;

    private string[] options = new[]
    {
        "eq",
        "lt",
        "lte",
        "gt",
        "gte"
    };

    public NodeCndWatchVibe(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 4)
        {
            sourceObject = entry[0].ToString();
            targetObject = entry[1].ToString();
            _ = TryParse(entry[2].ToString(), out vibe);
            var option = entry[3].ToString();
            var index = Array.FindIndex(options, s => s == option);
            if (index != -1)
            {
                modifierIndex = index;
            }
        }

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Condition, PinKind.Input));
        Outputs.Add(new NodePin(id++, "Trigger", this, LinkType.Trigger, PinKind.Output));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source Object", ref sourceObject);
        Controls.InputTextId("Target Object", ref targetObject);

        NodeActSetVibe.VibeComboBox(ref vibe);
        ImGui.Combo("Modifier", ref modifierIndex, options, options.Length);
    }
}
