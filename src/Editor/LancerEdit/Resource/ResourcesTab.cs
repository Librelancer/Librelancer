// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Resources;

namespace LancerEdit
{
    public class ResourcesTab : EditorTab
    {
        ResourceManager res;
        MainWindow win;
        Dictionary<string, ResourceUsage> resourceIndex;

        string typeFilter = "";
        string nameFilter = "";
        string usedByFilter = "";
        string sourceFilter = "";

        public ResourcesTab(MainWindow window, ResourceManager res, Dictionary<string, ResourceUsage> resourceIndex)
        {
            this.res = res;
            this.win = window;
            this.resourceIndex = resourceIndex;
            Title = "Resources";
        }

        public void SetUsedByFilter(string value)
        {
            usedByFilter = value ?? "";
        }

        public override void Draw(double elapsed)
        {
            var flags =
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.Resizable |
                ImGuiTableFlags.Sortable |
                ImGuiTableFlags.ScrollY;

            if (!ImGui.BeginTable("ResourcesTable", 4, flags))
                return;

            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 110);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Used By", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableHeadersRow();

            var sortedData = resourceIndex.Values.ToList();

            ApplySorting(sortedData);

            // Filter Row (locked top row)
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableSetColumnIndex(0);
            DrawFilterWithClearButton("typeFilter",ref typeFilter, ImGui.GetColumnWidth(), "Filter by Type");
            ImGui.TableSetColumnIndex(1);
            DrawFilterWithClearButton("nameFilter", ref nameFilter, ImGui.GetColumnWidth(), "Filter by  Name");
            ImGui.TableSetColumnIndex(2);
            DrawFilterWithClearButton("usedByFilter", ref usedByFilter, ImGui.GetColumnWidth(), "Filter by Used By");
            ImGui.TableSetColumnIndex(3);
            DrawFilterWithClearButton("sourceFilter", ref sourceFilter, ImGui.GetColumnWidth(), "Filter by Source");

            // Data Rows
            foreach (var usage in sortedData)
            {
                if (!PassesFilter(usage))
                    continue;

                ImGui.TableNextRow();

                DrawTypeCell(usage);
                DrawNameCell(usage);
                DrawUsedByCell(usage);
                DrawSourceCell(usage);
            }

            ImGui.EndTable();
        }

        unsafe void ApplySorting(List<ResourceUsage> list)
        {
            var sortSpecs = ImGui.TableGetSortSpecs();
            if (sortSpecs.SpecsCount == 0)
                return;

            var specsPtr = (ImGuiTableColumnSortSpecs*)sortSpecs.Specs.Handle;

            for (int i = 0; i < sortSpecs.SpecsCount; i++)
            {
                var spec = specsPtr[i];

                Comparison<ResourceUsage> comparison = null;

                switch (spec.ColumnIndex)
                {
                    case 0: // Type
                        comparison = (a, b) => a.Type.CompareTo(b.Type);
                        break;

                    case 1: // Name
                        comparison = (a, b) =>
                            string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                        break;

                    case 2: // Used By
                        comparison = (a, b) =>
                            string.Compare(
                                string.Join(",", a.UsedBy),
                                string.Join(",", b.UsedBy),
                                StringComparison.OrdinalIgnoreCase);
                        break;
                }

                if (comparison != null)
                {
                    if (spec.SortDirection == ImGuiSortDirection.Ascending)
                        list.Sort(comparison);
                    else
                        list.Sort((a, b) => comparison(b, a));
                }
            }

            // DO NOT gate sorting on SpecsDirty
            sortSpecs.SpecsDirty = false;
        }

        bool PassesFilter(ResourceUsage u)
        {
            if (!string.IsNullOrEmpty(typeFilter) &&
                !u.Type.ToString().Contains(typeFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(nameFilter) &&
                (u.Name == null ||
                 !u.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!string.IsNullOrEmpty(usedByFilter))
            {
                if (!u.UsedBy.Any(x =>
                        x.Contains(usedByFilter, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }

        void DrawTypeCell(ResourceUsage u)
        {
            ImGui.TableSetColumnIndex(0);

            Vector4 color = u.Missing
                ? Theme.ErrorTextColor
                : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            ImGui.TextColored(color, u.Type.ToString());
        }
        void DrawNameCell(ResourceUsage u)
        {
            ImGui.TableSetColumnIndex(1);

            Vector4 color = u.Missing
                ? Theme.ErrorTextColor
                : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            string label = u.Type == ResourceUsageType.Material
                ? $"{u.Name} (0x{u.MaterialId:X})"
                : u.Name;

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Selectable(ImGuiExt.IDSafe(label), false, ImGuiSelectableFlags.SpanAllColumns);
            ImGui.PopStyleColor();

            DrawContextMenu(label, u);
        }
        void DrawUsedByCell(ResourceUsage u)
        {
            ImGui.TableSetColumnIndex(2);

            Vector4 color = u.Missing
                ? Theme.ErrorTextColor
                : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            if (u.UsedBy.Count == 0)
            {
                ImGui.TextDisabled("-");
                return;
            }

            ImGui.TextColored(color,string.Join(", ", u.UsedBy.OrderBy(x => x)));
        }
        void DrawSourceCell(ResourceUsage u)
        {
            ImGui.TableSetColumnIndex(3);
            Vector4 color = u.Missing
                ? Theme.ErrorTextColor
                : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            if (u.ProvidedBy.Count == 0)
            {
                ImGui.TextDisabled("-");
                return;
            }

            ImGui.TextColored(color,string.Join(", ", u.ProvidedBy.OrderBy(x => x)));
        }
        void DrawFilterWithClearButton(string id, ref string value, float width, string hint)
        {
            ImGui.PushID(id);
            ImGui.Spacing();
            ImGui.SetNextItemWidth(width -ImGui.GetTextLineHeightWithSpacing()*2f); // leave space for button
            ImGui.InputTextWithHint("##input",hint, ref value, 128);


            ImGui.SameLine();

            //ImGui.SetNextItemWidth(36); // leave space for button

            if (ImGui.Button(Icons.X.ToString(), new Vector2(ImGui.GetTextLineHeightWithSpacing()*1.5f,ImGui.GetTextLineHeightWithSpacing())))
            {
                value = "";

            }
            ImGui.PopID();
        }
        void DrawContextMenu(string id, ResourceUsage u)
        {
            if (!ImGui.BeginPopupContextItem(id))
                return;

            if (u.Type == ResourceUsageType.Texture && !u.Missing)
            {
                if (ImGui.MenuItem("View Texture"))
                {
                    var tex = res.FindTexture(u.Name);
                    if (tex is Texture2D t2d)
                        win.AddTab(new TextureViewer(u.Name, t2d, null, false));
                }
            }

            if (u.Type == ResourceUsageType.Material && u.MaterialId.HasValue)
            {
                if (ImGui.MenuItem("Copy Material CRC"))
                    win.SetClipboardText($"0x{u.MaterialId.Value:X}");
            }

            ImGui.EndPopup();
        }

        static void SelectableColored(Vector4 col, string label)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, col);
            ImGui.Selectable(label);
            ImGui.PopStyleColor();
        }
        void ContextView(string name, Action onView)
        {
            if(ImGui.BeginPopupContextItem(name))
            {
                if (ImGui.MenuItem("View")) onView();
                ImGui.EndPopup();
            }
        }
    }
}
