using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Utf;
using LibreLancer.Utf.Anm;

namespace LancerEdit;

public class AddJointMap : PopupWindow
{
    public override string Title { get; set; } = "Add Joint Map";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private List<(AbstractConstruct Con, bool Disabled)> options = new();
    private int selectedIndex = -1;
    private Action<AbstractConstruct> onAdd;

    public AddJointMap(IEnumerable<AbstractConstruct> allParts, Script sc, Action<AbstractConstruct> onAdd)
    {
        this.onAdd = onAdd;
        foreach (var p in allParts)
        {
            if (p is FixConstruct) continue;
            bool used = sc.JointMaps.Any(x => x.ChildName.Equals(p.ChildName, StringComparison.OrdinalIgnoreCase));
            if (!used && selectedIndex == -1)
            {
                selectedIndex = options.Count;
            }
            options.Add((p, used));
        }
    }

    public override void Draw(bool appearing)
    {
        if (selectedIndex == -1)
        {
            ImGui.Text("All valid animation targets in use.");
            if (ImGui.Button("Ok"))
            {
                ImGui.CloseCurrentPopup();
            }
            return;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Target: ");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##target", options[selectedIndex].Con.ChildName))
        {
            for (int i = 0; i < options.Count; i++)
            {
                ImGui.BeginDisabled(options[i].Disabled);
                if (ImGui.Selectable(options[i].Con.ChildName))
                {
                    selectedIndex = i;
                }
                ImGui.EndDisabled();
            }
            ImGui.EndCombo();
        }
        if (ImGui.Button("Ok"))
        {
            onAdd(options[selectedIndex].Con);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
