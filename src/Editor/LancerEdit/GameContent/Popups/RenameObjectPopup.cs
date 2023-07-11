using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.World;
using SharpDX.DXGI;

namespace LancerEdit;

public class RenameObjectPopup : PopupWindow
{
    public override string Title { get; set; } = "Rename";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private string firstNickname;
    private string nickname;
    private GameWorld world;
    private Action<string> onSelect;
    
    public RenameObjectPopup(string initial, GameWorld world, Action<string> onSelect)
    {
        firstNickname = nickname = initial;
        this.world = world;
        this.onSelect = onSelect;
    }
    
    public override void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Nickname: ");
        ImGui.PushItemWidth(240);
        Controls.InputTextId("##nickname", ref nickname);
        ImGui.PopItemWidth();
        bool valid = true;
        if (string.IsNullOrWhiteSpace(nickname)) {
            ImGui.TextColored(Color4.Red, "Nickname cannot be empty");
            valid = false;
        }
        else if (!nickname.Equals(firstNickname, StringComparison.OrdinalIgnoreCase) &&
                 (world.GetObject(nickname) != null))
        {
            ImGui.TextColored(Color4.Red, "Nickname is already in use");
            valid = false;
        }
        if (ImGuiExt.Button("Ok", valid))
        {
            onSelect(nickname);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
    
}