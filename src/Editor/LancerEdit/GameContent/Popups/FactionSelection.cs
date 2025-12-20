using System;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer.Data.GameData;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class FactionSelection : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<Faction> onSelect;
    private FactionLookup lookup;

    public FactionSelection(Action<Faction> onSelect, string title, Faction initial, GameDataContext gd)
    {
        this.onSelect = onSelect;
        lookup = new FactionLookup("##factions", gd, initial);
        Title = title;
    }

    public override void Draw(bool appearing)
    {
        ImGui.PushItemWidth(300 * ImGuiHelper.Scale);
        lookup.Draw();
        ImGui.PopItemWidth();
        if (ImGui.Button("Ok"))
        {
            onSelect(lookup.Selected);
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

}
