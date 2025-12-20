using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.GameData;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class AsteroidSelection : PopupWindow
{
    public override string Title { get; set; } = "Asteroid";

    public override Vector2 InitSize => new Vector2(600, 400) * ImGuiHelper.Scale;

    public Asteroid[] Asteroids;
    private Asteroid[] compatibleAsteroids;

    private Action<Asteroid> onSelect;

    private GameDataContext gd;

    private uint compatibleCrc;

    public AsteroidSelection(Action<Asteroid> onSelect, GameDataContext gd, uint compatibleCrc)
    {
        this.onSelect = onSelect;
        Asteroids = gd.GameData.Items.Asteroids.OrderBy(x => x.Nickname).ToArray();
        this.gd = gd;
        this.compatibleCrc = compatibleCrc;
        if (compatibleCrc != 0)
        {
            compatibleAsteroids = Asteroids.Where(x => gd.GetAsteroidPreview(x).Item3 == compatibleCrc).ToArray();
            showIncompatible = false;
        }
    }

    private bool showIncompatible = true;
    public override void Draw(bool appearing)
    {
        if(compatibleCrc != 0)
            ImGui.Checkbox("Show incompatible asteroids", ref showIncompatible);
        ImGui.BeginChild("asteroids");
        var sel = DrawTable(showIncompatible ? Asteroids : compatibleAsteroids, gd);
        ImGui.EndChild();
        if (sel != null)
        {
            onSelect(sel);
            ImGui.CloseCurrentPopup();
        }
    }

    static bool SelectableImageButton(ImTextureRef image, Vector2 size, bool selected)
    {
        if(selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
        var retval = ImGui.ImageButton(image.ToString(), image, size, new Vector2(0, 1),
            new Vector2(1, 0));
        if(selected) ImGui.PopStyleColor();
        return retval;
    }

    public Asteroid DrawTable(Asteroid[] asteroids, GameDataContext gd)
    {
        ImGui.BeginTable("##table", 4);
        var clipper = new ImGuiListClipper();
        int itemsLen = asteroids.Length / 4;
        if (asteroids.Length % 4 != 0) itemsLen++;
        clipper.Begin(itemsLen);
        while (clipper.Step())
        {
            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                ImGui.TableNextRow();
                for (int i = row * 4; i < (row + 1) * 4 && i < asteroids.Length; i++)
                {
                    ImGui.TableNextColumn();
                    var (image, matName, matCrc) = gd.GetAsteroidPreview(asteroids[i]);
                    var cpos = ImGui.GetCursorPos();
                    if (SelectableImageButton(image, new Vector2(64) * ImGuiHelper.Scale, false))
                    {
                        return asteroids[i];
                    }
                    if(matCrc == compatibleCrc)
                        ImGui.SetItemTooltip($"Material: {matName} (0x{matCrc:X})");
                    else
                        ImGui.SetItemTooltip($"Material: {matName} (0x{matCrc:X}).\nWarning: Material does not match existing field");
                    ImGui.Text(asteroids[i].Nickname);
                    if (matCrc != compatibleCrc) {
                        ImGui.SetCursorPos(cpos + new Vector2(5 * ImGuiHelper.Scale));
                        ImGui.Text($"{Icons.Warning}");
                    }
                }
            }
        }
        ImGui.EndTable();
        return null;
    }

}
