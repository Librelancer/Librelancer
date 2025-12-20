using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.GameData;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class ArchetypeSelection : PopupWindow
{
    public override string Title { get; set; } = "Archetype";

    public override Vector2 InitSize => new Vector2(600, 400) * ImGuiHelper.Scale;

    private Action<Archetype> onSelect;


    private ArchetypeList list;

    public ArchetypeSelection(Action<Archetype> onSelect, Archetype initial, GameDataContext gd)
    {
        this.onSelect = onSelect;
        list = new ArchetypeList(gd, initial);
    }

    public override void Draw(bool appearing)
    {
        var sel = list.Draw("##archetypes");
        if (sel != null)
        {
            onSelect(sel);
            ImGui.CloseCurrentPopup();
        }
    }

}
