using System;
using System.Collections.Generic;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit.GameContent.Popups;

public sealed class ParentSelectPopup : PopupWindow
{
    public override string Title { get; set; } = "Set Parent";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;


    private SystemObjectLookup selection;
    private Action<GameObject> onSelect;
    private GameObject selected;

    public ParentSelectPopup(IEnumerable<GameObject> objects, GameDataContext ctx, GameObject initial,
        Action<GameObject> onSelect)
    {
        selection = new SystemObjectLookup(objects, ctx);
        selected = initial;
        this.onSelect = onSelect;
    }

    public override void Draw(bool appearing)
    {
        selection.Draw("Parent: ", ref selected, null, true);
        if (ImGui.Button("Ok"))
        {
            onSelect(selected);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

}
