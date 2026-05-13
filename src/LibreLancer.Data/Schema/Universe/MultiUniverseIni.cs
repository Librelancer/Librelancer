// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Universe;

public sealed class MultiUniverseIni
{
    public readonly Dictionary<string, MultiUniverseMap> Maps = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, string> Sectors = new(StringComparer.OrdinalIgnoreCase);
    public int TextureColumns { get; private set; } = 1;
    public int TextureRows { get; private set; } = 1;
    public MultiUniverseTile UniverseBackground { get; private set; } = new(1, 1);

    public MultiUniverseIni(string path, FileSystem vfs, IniStringPool stringPool)
    {
        foreach (var section in IniFile.ParseFile(path, vfs, true, stringPool))
        {
            if (section.Name.Equals("universe", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var entry in section)
                {
                    if (entry.Name.Equals("texture", StringComparison.OrdinalIgnoreCase) && entry.Count >= 2)
                    {
                        TextureColumns = Math.Max(1, ReadPositiveInt(entry, 0));
                        TextureRows = Math.Max(1, ReadPositiveInt(entry, 1));
                    }
                    else if (entry.Name.Equals("background", StringComparison.OrdinalIgnoreCase) && TryReadTile(entry, out var tile))
                    {
                        UniverseBackground = tile;
                    }
                    else if (entry.Name.Equals("sector", StringComparison.OrdinalIgnoreCase) && entry.Count >= 2)
                    {
                        Sectors[entry[0].ToString()] = entry[1].ToString();
                    }
                }
            }
            else if (section.Name.Equals("map", StringComparison.OrdinalIgnoreCase))
            {
                var map = new MultiUniverseMap();
                foreach (var entry in section)
                {
                    if (entry.Name.Equals("id", StringComparison.OrdinalIgnoreCase) && entry.Count > 0)
                    {
                        map.Id = entry[0].ToString();
                    }
                    else if (entry.Name.Equals("background", StringComparison.OrdinalIgnoreCase) && TryReadTile(entry, out var tile))
                    {
                        map.Background = tile;
                    }
                    else if ((entry.Name.Equals("system", StringComparison.OrdinalIgnoreCase) ||
                              entry.Name.Equals("sector", StringComparison.OrdinalIgnoreCase)) &&
                             TryReadMapMember(entry, out var member))
                    {
                        map.Members.Add(member);
                    }
                }

                if (!string.IsNullOrWhiteSpace(map.Id))
                    Maps[map.Id] = map;
            }
        }
    }

    public bool TryGetBackgroundUv(string mapId, out Vector2 uvMin, out Vector2 uvMax)
    {
        var tile = UniverseBackground;
        if (Maps.TryGetValue(mapId, out var map) && map.Background != null)
            tile = map.Background.Value;

        return TryTileToUv(tile, out uvMin, out uvMax);
    }

    private bool TryTileToUv(MultiUniverseTile tile, out Vector2 uvMin, out Vector2 uvMax)
    {
        var column = Math.Clamp(tile.Column, 1, TextureColumns);
        var row = Math.Clamp(tile.Row, 1, TextureRows);
        var cellW = 1f / TextureColumns;
        var cellH = 1f / TextureRows;
        var left = (column - 1) * cellW;
        var right = column * cellW;
        var top = (row - 1) * cellH;
        var bottom = row * cellH;

        uvMin = new Vector2(left, 1f - top);
        uvMax = new Vector2(right, 1f - bottom);
        return true;
    }

    private static bool TryReadTile(Entry entry, out MultiUniverseTile tile)
    {
        tile = default;
        if (entry.Count < 2)
            return false;

        tile = new MultiUniverseTile(ReadPositiveInt(entry, 0), ReadPositiveInt(entry, 1));
        return true;
    }

    private static bool TryReadMapMember(Entry entry, out MultiUniverseMapMember member)
    {
        member = default;
        if (entry.Count < 3 ||
            !entry[1].TryToSingle(out var x) ||
            !entry[2].TryToSingle(out var y))
            return false;

        member = new MultiUniverseMapMember(entry[0].ToString(), x, y);
        return true;
    }

    private static int ReadPositiveInt(Entry entry, int index)
    {
        if (entry[index].TryToInt32(out var intValue))
            return Math.Max(1, intValue);
        if (entry[index].TryToSingle(out var floatValue))
            return Math.Max(1, (int)MathF.Round(floatValue));
        return 1;
    }
}

public sealed class MultiUniverseMap
{
    public string Id = string.Empty;
    public MultiUniverseTile? Background;
    public readonly List<MultiUniverseMapMember> Members = [];
}

public readonly record struct MultiUniverseTile(int Column, int Row);

public readonly record struct MultiUniverseMapMember(string Nickname, float X, float Y);
