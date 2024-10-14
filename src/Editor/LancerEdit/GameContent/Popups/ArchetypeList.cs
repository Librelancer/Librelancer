using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.GameData;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class ArchetypeList
{
    public Archetype Selected;

    private string filterText = "";
    private Archetype[] displayList;
    private Archetype[] fullList;
    private GameDataContext gd;

    private bool doFiltering = false;

    private ImGuiInputTextCallback textCallback;

    unsafe int OnTextChanged(ImGuiInputTextCallbackData* d)
    {
        doFiltering = true;
        return 0;
    }

    public unsafe ArchetypeList(GameDataContext gd, Archetype selected)
    {
        displayList = fullList = gd.GameData.Archetypes.OrderBy(x => x.Nickname).ToArray();
        textCallback = OnTextChanged;
        this.Selected = selected;
        this.gd = gd;
    }

    static bool SelectableImageButton(int image, Vector2 size, bool selected)
    {
        if (selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
        var retval = ImGui.ImageButton(image.ToString(), (IntPtr)image, size, new Vector2(0, 1),
            new Vector2(1, 0));
        if (selected) ImGui.PopStyleColor();
        return retval;
    }

    public Archetype Draw(string id)
    {
        ImGui.PushID(id);
        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##filter", "Filter", ref filterText, 250, ImGuiInputTextFlags.CallbackEdit,
            textCallback);
        ImGui.PopItemWidth();
        if (doFiltering)
        {
            if (string.IsNullOrEmpty(filterText))
            {
                displayList = fullList;
            }
            else
            {
                displayList = fullList.Where(
                        x => x == Selected || x.Nickname.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            doFiltering = false;
        }

        Archetype returnValue = null;
        ImGui.BeginChild("##archetypes", new Vector2(ImGui.GetWindowWidth() - 12 * ImGuiHelper.Scale,
            ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetFrameHeightWithSpacing() - 8 * ImGuiHelper.Scale), ImGuiChildFlags.Border);
        ImGui.BeginTable("##table", 4);
        using var clipper = new ListClipper();
        int itemsLen = displayList.Length / 4;
        if (displayList.Length % 4 != 0) itemsLen++;
        clipper.Begin(itemsLen);
        while (clipper.Step())
        {
            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                ImGui.TableNextRow();
                for (int i = row * 4; i < (row + 1) * 4 && i < displayList.Length; i++)
                {
                    ImGui.TableNextColumn();
                    var image = gd.GetArchetypePreview(displayList[i]);
                    if (SelectableImageButton(image, new Vector2(64) * ImGuiHelper.Scale, displayList[i] == Selected))
                    {
                        Selected = returnValue = displayList[i];
                        doFiltering = true;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(displayList[i].Nickname);
                        ImGui.Separator();
                        ImGui.TextUnformatted($"Type: {displayList[i].Type}");
                        ImGui.EndTooltip();
                    }

                    ImGui.Text(displayList[i].Nickname);
                }
            }
        }

        ImGui.EndTable();
        ImGui.EndChild();
        ImGui.PopID();
        return returnValue;
    }
}
