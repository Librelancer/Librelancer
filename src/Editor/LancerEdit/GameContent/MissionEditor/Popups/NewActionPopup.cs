using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.Popups;

public class NewActionPopup(Action<TriggerActions> onCreate) : PopupWindow
{
    public override string Title { get; set; } = "New Action";

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private int selection = 0;
    private TriggerActions selected = _options[0].Value;

    private static readonly (string Text, TriggerActions Value)[] _options =
        Enum.GetValues<TriggerActions>()
            .Where(x => !ScriptedAction.Unsupported.Contains(x))
            .Select(x => (x.ToString(), x))
            .OrderBy(x => x.Item1)
            .ToArray();

    public override void Draw(bool appearing)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Action: ");
        if (ImGui.BeginCombo("##label", _options[selection].Text))
        {
            for (var i = 0; i < _options.Length; i++)
            {
                if (!ImGui.Selectable(_options[i].Text))
                {
                    continue;
                }

                selection = i;
                selected = _options[i].Value;
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
