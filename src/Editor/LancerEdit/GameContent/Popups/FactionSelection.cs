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
    private ObjectLookup<Faction> lookup;
    private Faction selected;

    public FactionSelection(Action<Faction> onSelect, string title, Faction initial, GameDataContext gd)
    {
        this.onSelect = onSelect;
        lookup = gd.Factions;
        Title = title;
        selected = initial;
    }

    public override void Draw(bool appearing)
    {
        ImGui.PushItemWidth(300 * ImGuiHelper.Scale);
        lookup.Draw("##faction", ref selected, null, true);
        ImGui.PopItemWidth();
        if (ImGui.Button("Ok"))
        {
            onSelect(selected);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }

}
