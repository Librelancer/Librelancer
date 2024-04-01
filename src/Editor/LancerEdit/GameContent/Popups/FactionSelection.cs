using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.GameData;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class FactionSelection : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<Faction> onSelect;
    private string[] names;
    private Faction[] factions;
    private int selectedIndex;
    
    public FactionSelection(Action<Faction> onSelect, string title, Faction initial, GameDataContext gd)
    {
        this.onSelect = onSelect;
        factions = gd.GameData.Factions.OrderBy(x => x.Nickname).ToArray();
        names = factions.Select(x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})").ToArray();
        selectedIndex = Array.IndexOf(factions, initial);
        Title = title;
    }

    public override void Draw()
    {
        ImGui.PushItemWidth(200 * ImGuiHelper.Scale);
        ImGui.Combo("##factions", ref selectedIndex, names, names.Length);
        ImGui.PopItemWidth();
        if (ImGui.Button("Ok"))
        {
            onSelect(GetSelection());
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            onSelect(null);
                ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }

    Faction GetSelection() =>
        selectedIndex >= 0 && selectedIndex < factions.Length
            ? factions[selectedIndex]
            : null;
}