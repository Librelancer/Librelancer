using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.Popups;

public class NewConditionPopup(Action<TriggerConditions> onCreate) : PopupWindow
{
    public override string Title { get; set; } = "New Action";

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private int selection = 0;
    private TriggerConditions selected = options[0].Value;

    private static readonly (string Text, TriggerConditions Value)[] options =
        Enum.GetValues<TriggerConditions>()
            .Where(x => !ScriptedCondition.Unsupported.Contains(x))
            .Select(x => (x.ToString(), x))
            .OrderBy(x => x.Item1)
            .ToArray();

    public override void Draw(bool appearing)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Condition: ");
        if (ImGui.BeginCombo("##label", options[selection].Text))
        {
            for (var i = 0; i < options.Length; i++)
            {
                if (!ImGui.Selectable(options[i].Text))
                {
                    continue;
                }

                selection = i;
                selected = options[i].Value;
            }
            ImGui.EndCombo();
        }

        if (ImGui.Button("Ok"))
        {
            onCreate(selected);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
