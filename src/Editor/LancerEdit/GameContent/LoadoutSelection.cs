using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit;

public class LoadoutSelection : PopupWindow
{
    public override string Title { get; set; } = "Loadout";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private ObjectLoadout[] loadouts;
    private string[] names;
    private int selectedIndex = -1;

    private Action<ObjectLoadout> onSelect;
    
    public LoadoutSelection(Action<ObjectLoadout> onSelect, ObjectLoadout initial, GameDataContext gd)
    {
        loadouts = gd.GameData.Loadouts.ToArray();
        names = loadouts.Select(x => x.Nickname).ToArray();
        this.onSelect = onSelect;
        if (initial != null)
        {
            for (int i = 0; i < loadouts.Length; i++) {
                if (loadouts[i] == initial)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        
    }

    public override void Draw()
    {
        ImGui.Combo("##item", ref selectedIndex, names, names.Length);
        if (selectedIndex != -1)
        {
            ImGui.Separator();
            var x = loadouts[selectedIndex];
            ImGui.BeginChild("##items", new Vector2(400, 400));
            if(!string.IsNullOrEmpty(x.Archetype))
                ImGui.TextUnformatted($"Archetype: {x.Archetype}");
            foreach (var item in x.Items)
            {
                ImGui.TextUnformatted($"{item.Hardpoint}: {item.Equipment.Nickname}");
            }
            foreach (var item in x.Cargo)
            {
                ImGui.TextUnformatted($"Cargo: {item.Item.Nickname} x{item.Count}");
            }
            ImGui.EndChild();
        }

        if (ImGui.Button("Ok")) {
            if(selectedIndex != -1)
                onSelect?.Invoke(loadouts[selectedIndex]);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            onSelect?.Invoke(null);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}