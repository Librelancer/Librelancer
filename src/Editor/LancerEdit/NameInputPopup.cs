using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit;

public class NameInputConfig
{
    public string Title;
    public string ValueName;
    public bool AllowInitial = true;
    public Func<string, bool> InUse;
    public Action Extra;
    public InputFilter Filter = Controls.IdFilter;
    public bool AllowEmpty = false;
    public static NameInputConfig Nickname(string title, Func<string, bool> inUse) => new() {Title = title, ValueName = "Nickname", InUse = inUse};
    public static NameInputConfig Rename() => new() {Title = "Rename", Filter = null};
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

    public override void Draw(bool appearing)
    {
        if (config.ValueName != null)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{config.ValueName}: ");
            ImGui.SameLine();
        }
        if (appearing)
        {
            ImGui.SetKeyboardFocusHere();
        }
        bool entered = Controls.InputTextFilter("##nickname", ref nickname, config.Filter, 240);
        bool valid = true;
        if (string.IsNullOrWhiteSpace(nickname) && !config.AllowEmpty) {
            ImGui.TextColored(Color4.Red, $"{config.ValueName ?? "Name"} cannot be empty");
            valid = false;
        }
        else if (config.InUse != null && (!config.AllowInitial || !nickname.Equals(firstNickname, StringComparison.OrdinalIgnoreCase)) &&
                config.InUse(nickname))
        {
            ImGui.TextColored(Color4.Red, $"{config.ValueName ?? "Name"} is already in use");
            valid = false;
        }
        config.Extra?.Invoke();
        if (ImGuiExt.Button("Ok", valid) || (valid && entered))
        {
            onSelect(nickname);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }

}
