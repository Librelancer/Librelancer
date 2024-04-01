using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class ColorPicker : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;
    private Color4 color;

    private Action<Color4> onSelect;
    
    public ColorPicker(string title, Color4 initial, Action<Color4> onSelect)
    {
        color = initial;
        Title = title;
        this.onSelect = onSelect;
    }

    public override void Draw()
    {
        ImGuiExt.ColorPicker3("##color", ref color);
        if (ImGui.Button("Ok")) {
            onSelect(color);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) {
            ImGui.CloseCurrentPopup();
        }
    }
}