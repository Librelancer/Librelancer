using System;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer.Data.GameData;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class NewsSelection : PopupWindow
{
    public override string Title { get; set; } = "Add News";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<NewsItem> onSelect;
    private NewsItemLookup lookup;

    public NewsSelection(
        Action<NewsItem> onSelect,
        NewsCollection news,
        GameDataContext gd,
        Func<NewsItem, bool> allow)
    {
        this.onSelect = onSelect;
        lookup = new NewsItemLookup("##NewsItems", news, gd, allow);
    }

    public override void Draw(bool appearing)
    {
        var width = 300 * ImGuiHelper.Scale;
        ImGui.PushItemWidth(width);
        lookup.Draw();
        ImGui.PopItemWidth();
        if (ImGuiExt.Button("Ok", lookup.Selected != null))
        {
            onSelect(lookup.Selected);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }

}
