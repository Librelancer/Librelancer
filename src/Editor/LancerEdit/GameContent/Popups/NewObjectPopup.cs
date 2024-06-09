using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.GameData;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit.GameContent.Popups;

public class NewObjectPopup : PopupWindow
{
    public override string Title { get; set; } = "New Object";

    public override Vector2 InitSize => new Vector2(600, 400);

    public Archetype[] Archetypes;

    private GameDataContext gd;

    private string nickname = "";

    private Action<string, Archetype, Vector3?> onCreate;

    private Archetype selectedArchetype;

    private GameWorld world;

    public Vector3? Position;

    public NewObjectPopup(GameDataContext gd, GameWorld world, Vector3? position, Action<string, Archetype, Vector3?> onCreate)
    {
        Archetypes = gd.GameData.Archetypes.OrderBy(x => x.Nickname).ToArray();
        this.gd = gd;
        this.world = world;
        this.onCreate = onCreate;
        this.Position = position;
    }

    public override void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Nickname: ");
        ImGui.InputText("##nickname", ref nickname, 100);
        ImGui.Separator();
        ImGui.BeginChild("##archetypes", new Vector2(ImGui.GetWindowWidth() - 12 * ImGuiHelper.Scale,
            ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetFrameHeightWithSpacing() - 8 * ImGuiHelper.Scale), ImGuiChildFlags.Border);
        var a = ArchetypeSelection.DrawTable(Archetypes, gd, selectedArchetype);
        if (a != null) selectedArchetype = a;
        ImGui.EndChild();

        var n = nickname.Trim();
        if (ImGuiExt.Button("Create", !string.IsNullOrWhiteSpace(n) && selectedArchetype != null &&
                                  world.GetObject(n) == null))
        {
            onCreate(nickname, selectedArchetype, Position);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}
