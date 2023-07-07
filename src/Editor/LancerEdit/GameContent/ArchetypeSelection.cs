using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit;

public class ArchetypeSelection : PopupWindow
{
    public override string Title { get; set; } = "Archetype";

    public override Vector2 InitSize => new Vector2(600, 400);

    public Archetype[] Archetypes;
    
    private Action<Archetype> onSelect;

    private GameDataContext gd;

    public ArchetypeSelection(Action<Archetype> onSelect, Archetype initial, GameDataContext gd)
    {
        this.onSelect = onSelect;
        Archetypes = gd.GameData.Archetypes.ToArray();
        this.gd = gd;
    }

    public override void Draw()
    {
        ImGui.BeginTable("##table", 4);
        using var clipper = new ListClipper();
        int itemsLen = Archetypes.Length / 4;
        if (Archetypes.Length % 4 != 0) itemsLen++;
        clipper.Begin(itemsLen);
        while (clipper.Step())
        {
            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                ImGui.TableNextRow();
                for (int i = row * 4; i < (row + 1) * 4 && i < Archetypes.Length; i++)
                {
                    ImGui.TableNextColumn();
                    var image = gd.GetArchetypePreview(Archetypes[i]);
                    if (ImGui.ImageButton((IntPtr) image, new Vector2(64) * ImGuiHelper.Scale, new Vector2(0, 1),
                            new Vector2(1, 0)))
                    {
                        onSelect(Archetypes[i]);
                        ImGui.CloseCurrentPopup();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(Archetypes[i].Nickname);
                        ImGui.Separator();
                        ImGui.TextUnformatted($"Type: {Archetypes[i].Type}");
                        ImGui.EndTooltip();
                    }
                    ImGui.Text(Archetypes[i].Nickname);
                }
            }
        }
        ImGui.EndTable();
    }
}