using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Filters;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class LoadoutSelection : PopupWindow
{
    public override string Title { get; set; } = "Loadout";
    public override Vector2 InitSize => new(600, 350);

    private ObjectLoadout[] allLoadouts;
    private ObjectLoadout[] filteredLoadouts;
    private ObjectLoadout selected;
    private bool scrollToSelected = true;
    private int selectedIndex = -1;
    private bool filterCompatible = false;
    private string filterText = "";

    private Action<ObjectLoadout> onSelect;

    private ObjectFiltering<ObjectLoadout> filters;

    private GameDataContext gd;

    public unsafe LoadoutSelection(Action<ObjectLoadout> onSelect, ObjectLoadout initial, string[] hardpoints,
        GameDataContext gd)
    {
        allLoadouts = filteredLoadouts = gd.GameData.Items.Loadouts.OrderBy(x => x.Nickname).ToArray();
        filters = new LoadoutFilters(hardpoints);
        selected = initial;
        this.gd = gd;
        this.onSelect = onSelect;
        textCallback = OnTextChanged;
    }

    private bool doFiltering = false;

    private ImGuiInputTextCallback textCallback;

    unsafe int OnTextChanged(ImGuiInputTextCallbackData* d)
    {
        doFiltering = true;
        return 0;
    }

    public override void Draw(bool appearing)
    {
        if (doFiltering)
            filteredLoadouts = filters.Filter(filterText, allLoadouts, filterCompatible ? "compatible" : "").ToArray();
        var frameH = ImGui.GetFrameHeightWithSpacing();
        var windowH = ImGui.GetWindowHeight();

        ImGui.BeginTable("##parent", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInner);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##filter", "Filter", ref filterText, 250, ImGuiInputTextFlags.CallbackEdit,
            textCallback);
        ImGui.PopItemWidth();
        if (ImGui.Checkbox("Hide Incompatible", ref filterCompatible))
            doFiltering = true;
        ImGui.BeginChild("##loadouts", new Vector2(-1, windowH - frameH * 5f));
        foreach (var l in filteredLoadouts)
        {
            if (selected == l && scrollToSelected)
            {
                ImGui.SetScrollHereY();
            }

            if (ImGui.Selectable(l.Nickname, selected == l, ImGuiSelectableFlags.AllowDoubleClick))
            {
                selected = l;
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    onSelect(l);
                    ImGui.CloseCurrentPopup();
                }
            }
        }

        scrollToSelected = false;
        ImGui.EndChild();
        ImGui.TableNextColumn();
        if (selected != null)
        {
            ImGui.BeginChild("##items", new Vector2(-1, windowH - frameH * 3f));
            ImGui.Text($"Archetype: {selected.Archetype}");
            foreach (var item in selected.Items)
            {
                ImGui.Text(
                    $"{item.Hardpoint}: '{gd.Infocards.GetStringResource(item.Equipment.IdsName)}' ({item.Equipment.Nickname})");
            }

            foreach (var item in selected.Cargo)
            {
                ImGui.Text(
                    $"Cargo: '{gd.Infocards.GetStringResource(item.Item.IdsName)}' ({item.Item.Nickname}) x{item.Count}");
            }

            ImGui.EndChild();
        }
        else
        {
            ImGui.Text("No selection");
        }

        ImGui.EndTable();
        ImGui.Separator();
        if (ImGui.Button("Ok"))
        {
            onSelect?.Invoke(selected);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            onSelect?.Invoke(null);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}
