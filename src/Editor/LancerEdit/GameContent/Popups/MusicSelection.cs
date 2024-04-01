using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Audio;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class MusicSelection : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<string> onSelect;
    private string[] music;
    private GameDataContext gd;
    private int selectedIndex;
    private MainWindow win;
    
    public MusicSelection(Action<string> onSelect, string title, string initial, GameDataContext gd, MainWindow win)
    {
        this.onSelect = onSelect;
        music = gd.GameData.AllSounds.Where(x => x.Type == AudioType.Music)
            .OrderBy(x => x.Nickname)
            .Select(x => x.Nickname).ToArray();
        selectedIndex = Array.IndexOf(music, initial);
        Title = title;
        this.win = win;
        this.gd = gd;
    }

    public override void Draw()
    {
        ImGui.PushItemWidth(200 * ImGuiHelper.Scale);
        ImGui.Combo("##music", ref selectedIndex, music, music.Length);
        ImGui.PopItemWidth();
        var sel = GetSelection();
        ImGui.SameLine();
        if(Controls.Music("music", win, sel != null))
            gd.Sounds.PlayMusic(sel, 0, true);
        if (ImGui.Button("Ok"))
        {
            onSelect(sel);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }

    string GetSelection() =>
        selectedIndex >= 0 && selectedIndex < music.Length
            ? music[selectedIndex]
            : null;
}
