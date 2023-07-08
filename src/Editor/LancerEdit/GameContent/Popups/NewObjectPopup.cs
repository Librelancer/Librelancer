using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.GameData;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit;

public class NewObjectPopup : PopupWindow
{
    public override string Title { get; set; } = "New Object";

    public override Vector2 InitSize => new Vector2(600, 400);

    public Archetype[] Archetypes;
    
    private GameDataContext gd;

    private string nickname = "";

    private Action<string, Archetype> onCreate;

    private Archetype selectedArchetype;

    private GameWorld world;

    public NewObjectPopup(GameDataContext gd, GameWorld world, Action<string, Archetype> onCreate)
    {
        Archetypes = gd.GameData.Archetypes.OrderBy(x => x.Nickname).ToArray();
        this.gd = gd;
        this.world = world;
        this.onCreate = onCreate;
    }

    public override void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Nickname: ");
        ImGui.InputText("##nickname", ref nickname, 100);
        ImGui.Separator();
        ImGui.BeginChild("##archetypes", new Vector2(ImGui.GetWindowWidth(), 
            ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetFrameHeightWithSpacing() - 8 * ImGuiHelper.Scale), true);
        var a = ArchetypeSelection.DrawTable(Archetypes, gd, selectedArchetype);
        if (a != null) selectedArchetype = a;
        ImGui.EndChild();

        var n = nickname.Trim();
        if (ImGuiExt.Button("Create", !string.IsNullOrWhiteSpace(n) && selectedArchetype != null &&
                                  world.GetObject(n) == null))
        {
            onCreate(nickname, selectedArchetype);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}