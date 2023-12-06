using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.World;
using SharpDX.DXGI;

namespace LancerEdit;

public class NameInputConfig
{
    public string Title;
    public string ValueName;
    public Func<string, bool> InUse;
    public bool IsId = true;
    public static NameInputConfig Nickname(string title, Func<string, bool> inUse) => new() {Title = title, ValueName = "Nickname", InUse = inUse};
    public static NameInputConfig Rename() => new() {Title = "Rename", IsId = false};
}

public class NameInputPopup : PopupWindow
{
    public override string Title
    {
        get => config.Title;
        set => config.Title = value;
    }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private string firstNickname;
    private string nickname;
    private Action<string> onSelect;

    private NameInputConfig config;


    public NameInputPopup(NameInputConfig config, string initial, Action<string> onSelect)
    {
        this.config = config;
        firstNickname = nickname = initial;
        this.onSelect = onSelect;
    }

    public override void Draw()
    {
        if (config.ValueName != null)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{config.ValueName}: ");
            ImGui.SameLine();
        }
        ImGui.PushItemWidth(240);
        if (config.IsId)
            Controls.InputTextId("##nickname", ref nickname);
        else
            ImGui.InputText("##nickname", ref nickname, 250);
        ImGui.PopItemWidth();
        bool valid = true;
        if (string.IsNullOrWhiteSpace(nickname)) {
            ImGui.TextColored(Color4.Red, $"{config.ValueName ?? "Name"} cannot be empty");
            valid = false;
        }
        else if (config.InUse != null && !nickname.Equals(firstNickname, StringComparison.OrdinalIgnoreCase) &&
                config.InUse(nickname))
        {
            ImGui.TextColored(Color4.Red, $"{config.ValueName ?? "Name"} is already in use");
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
