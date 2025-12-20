using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.GameData;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit.GameContent.Popups;

public class NewObjectPopup : PopupWindow
{
    public override string Title { get; set; } = "New Object";

    public override Vector2 InitSize => new Vector2(600, 400);

    private string nickname = "";

    private Action<string, Archetype, Vector3?> onCreate;

    private GameWorld world;

    public Vector3? Position;

    private ArchetypeList list;

    public NewObjectPopup(GameDataContext gd, GameWorld world, Vector3? position, Action<string, Archetype, Vector3?> onCreate)
    {
        list = new ArchetypeList(gd, null);
        this.world = world;
        this.onCreate = onCreate;
        this.Position = position;
    }

    public override void Draw(bool appearing)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Nickname: ");
        ImGui.SameLine();
        Controls.InputTextIdDirect("##nickname", ref nickname);
        ImGui.Separator();
        ImGui.Text("Archetype:");
        list.Draw("##archetypes");

        if (ImGuiExt.Button("Create", !string.IsNullOrWhiteSpace(nickname) && list.Selected != null &&
                                  world.GetObject(nickname) == null))
        {
            onCreate(nickname, list.Selected, Position);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}
