using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class BaseSelection : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<Base> onSelect;
    private string[] names;
    private Base[] Bases;
    private int selectedIndex;
    private string message;
    
    public BaseSelection(Action<Base> onSelect, string title, string message, Base initial, GameDataContext gd)
    {
        this.message = message;
        this.onSelect = onSelect;
        Bases = gd.GameData.Bases.OrderBy(x => x.Nickname).ToArray();
        names = Bases.Select(x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})").ToArray();
        selectedIndex = Array.IndexOf(Bases, initial);
        Title = title;
    }
    
    public override void Draw()
    {
        var width = 200 * ImGuiHelper.Scale;
        if (message != null) {
            ImGui.TextUnformatted(message);
            width = Math.Max(width, ImGui.CalcTextSize(message).X);
        }
        ImGui.PushItemWidth(width);
        ImGui.Combo("##Bases", ref selectedIndex, names, names.Length);
        ImGui.PopItemWidth();
        if (ImGui.Button("Ok"))
        {
            onSelect(GetSelection());
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            onSelect(null);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }

    Base GetSelection() =>
        selectedIndex >= 0 && selectedIndex < Bases.Length
            ? Bases[selectedIndex]
            : null;
}