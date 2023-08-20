using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.World;
using SharpDX.DXGI;

namespace LancerEdit;

public class NicknameInputPopup : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private string firstNickname;
    private string nickname;
    private Func<string,bool> inUse;
    private Action<string> onSelect;
    
    
    public NicknameInputPopup(string title, string initial, Func<string,bool> inUse, Action<string> onSelect)
    {
        this.Title = title;
        firstNickname = nickname = initial;
        this.inUse = inUse;
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
                inUse(nickname))
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