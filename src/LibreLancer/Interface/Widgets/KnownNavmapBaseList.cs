// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

internal sealed class NavmapBaseListItem
{
    public string Name = "";
    public string SystemName = "";
    public uint SystemHash;
    public uint ObjectHash;
}

[WattleScriptUserData]
public sealed class KnownNavmapBaseList : ITableData
{
    private readonly NavmapBaseListItem[] items;

    internal KnownNavmapBaseList(NavmapBaseListItem[] items)
    {
        this.items = items is { Length: > 0 } ? (NavmapBaseListItem[])items.Clone() : [];
        Sort("name");
    }

    public int Count => items.Length;

    public int Selected { get; set; } = -1;

    public uint SelectedSystemHash => ValidSelection() ? items[Selected].SystemHash : 0;

    public uint SelectedObjectHash => ValidSelection() ? items[Selected].ObjectHash : 0;

    public string GetContentString(int row, string column)
    {
        if (row < 0 || row >= items.Length)
            return "";

        return column switch
        {
            "name" => items[row].Name,
            "system" => items[row].SystemName,
            _ => ""
        };
    }

    public void Sort(string column)
    {
        if (column != "name" && column != "system")
            return;

        Array.Sort(items, (left, right) =>
        {
            var comparison = string.Compare(
                SortValue(left, column),
                SortValue(right, column),
                StringComparison.OrdinalIgnoreCase);
            if (comparison != 0)
                return comparison;

            comparison = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            if (comparison != 0)
                return comparison;

            return string.Compare(left.SystemName, right.SystemName, StringComparison.OrdinalIgnoreCase);
        });

        Selected = -1;
    }

    public bool ValidSelection() => Selected >= 0 && Selected < items.Length;

    private static string SortValue(NavmapBaseListItem item, string column) =>
        column == "system" ? item.SystemName : item.Name;
}
