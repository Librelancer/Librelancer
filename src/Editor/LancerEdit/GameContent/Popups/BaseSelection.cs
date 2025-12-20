using System;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class BaseSelection : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<Base> onSelect;
    private BaseLookup lookup;
    private string message;
    private bool needsValue;

    public BaseSelection(Action<Base> onSelect,
        string title,
        string message,
        Base initial,
        GameDataContext gd,
        Func<Base, bool> allow = null,
        bool needsValue = false)
    {
        this.message = message;
        this.onSelect = onSelect;
        lookup = new BaseLookup("##Bases", gd, initial, allow);
        Title = title;
        this.needsValue = needsValue;
    }

    public override void Draw(bool appearing)
    {
        var width = 300 * ImGuiHelper.Scale;
        if (message != null) {
            ImGui.Text(message);
            width = Math.Max(width, ImGui.CalcTextSize(message).X);
        }
        ImGui.PushItemWidth(width);
        lookup.Draw();
        ImGui.PopItemWidth();
        if (ImGuiExt.Button("Ok", !needsValue || lookup.Selected != null))
        {
            onSelect(lookup.Selected);
            ImGui.CloseCurrentPopup();
        }
        if (!needsValue)
        {
            ImGui.SameLine();
            if (ImGui.Button("Clear"))
            {
                onSelect(null);
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }

}
