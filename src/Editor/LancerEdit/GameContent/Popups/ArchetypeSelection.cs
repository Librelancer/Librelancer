using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.GameData;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class ArchetypeSelection : PopupWindow
{
    public override string Title { get; set; } = "Archetype";

    public override Vector2 InitSize => new Vector2(600, 400);

    public Archetype[] Archetypes;

    private Action<Archetype> onSelect;

    private Archetype initial;

    private GameDataContext gd;

    public ArchetypeSelection(Action<Archetype> onSelect, Archetype initial, GameDataContext gd)
    {
        this.onSelect = onSelect;
        Archetypes = gd.GameData.Archetypes.OrderBy(x => x.Nickname).ToArray();
        this.initial = initial;
        this.gd = gd;
    }

    public override void Draw()
    {
        var sel = DrawTable(Archetypes, gd, initial);
        if (sel != null)
        {
            onSelect(sel);
            ImGui.CloseCurrentPopup();
        }
    }

    static bool SelectableImageButton(int image, Vector2 size, bool selected)
    {
        if(selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
        var retval = ImGui.ImageButton(image.ToString(), (IntPtr) image, size, new Vector2(0, 1),
            new Vector2(1, 0));
        if(selected) ImGui.PopStyleColor();
        return retval;
    }

    public static Archetype DrawTable(Archetype[] archetypes, GameDataContext gd, Archetype selected)
    {
        ImGui.BeginTable("##table", 4);
        using var clipper = new ListClipper();
        int itemsLen = archetypes.Length / 4;
        if (archetypes.Length % 4 != 0) itemsLen++;
        clipper.Begin(itemsLen);
        while (clipper.Step())
        {
            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                ImGui.TableNextRow();
                for (int i = row * 4; i < (row + 1) * 4 && i < archetypes.Length; i++)
                {
                    ImGui.TableNextColumn();
                    var image = gd.GetArchetypePreview(archetypes[i]);
                    if (SelectableImageButton(image, new Vector2(64) * ImGuiHelper.Scale, archetypes[i] == selected))
                    {
                        return archetypes[i];
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(archetypes[i].Nickname);
                        ImGui.Separator();
                        ImGui.TextUnformatted($"Type: {archetypes[i].Type}");
                        ImGui.EndTooltip();
                    }
                    ImGui.Text(archetypes[i].Nickname);
                }
            }
        }
        ImGui.EndTable();
        return null;
    }

}
