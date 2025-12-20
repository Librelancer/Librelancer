using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Schema.Audio;
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

    private string selection;

    private SearchDropdown<string> dropdown;

    public MusicSelection(Action<string> onSelect, string title, string initial, GameDataContext gd, MainWindow win)
    {
        dropdown = new SearchDropdown<string>("##music", x => x ?? "(none)", x => selection = x, initial,
            gd.GameData.AllSounds.Where(x => x.Type == AudioType.Music)
                .OrderBy(x => x.Nickname)
                .Select(x => x.Nickname).ToArray());
        this.onSelect = onSelect;
        this.selection = initial;
        Title = title;
        this.win = win;
        this.gd = gd;
    }

    public override void Draw(bool appearing)
    {
        ImGui.PushItemWidth(200 * ImGuiHelper.Scale);
        dropdown.Draw();
        ImGui.PopItemWidth();
        ImGui.SameLine();
        if(Controls.Music("music", win, selection != null))
            gd.Sounds.PlayMusic(selection, 0, true);
        if (ImGui.Button("Ok"))
        {
            onSelect(selection);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}
