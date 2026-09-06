// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Linq;
using LibreLancer.Client;
using LibreLancer.Data;
using LibreLancer.Interface;

namespace LibreLancer;

internal static class KnownNavmapBaseListBuilder
{
    public static KnownNavmapBaseList Build(GameDataManager gameData, CGameSession session)
    {
        var items = gameData.Items;
        var bases = items.Bases
            .Select(b =>
            {
                var system = !string.IsNullOrWhiteSpace(b.System) ? items.Systems.Get(b.System) : null;
                var obj = system?.Objects.FirstOrDefault(x => x.Base == b);
                if (system == null || obj == null || !session.IsVisited(FLHash.CreateID(obj.Nickname)))
                    return null;

                var baseName = gameData.GetString(b.IdsName);
                var systemName = gameData.GetString(system.IdsName);
                return new NavmapBaseListItem
                {
                    Name = string.IsNullOrWhiteSpace(baseName) ? b.Nickname : baseName,
                    SystemName = string.IsNullOrWhiteSpace(systemName) ? system.Nickname : systemName,
                    SystemHash = system.CRC,
                    ObjectHash = FLHash.CreateID(obj.Nickname)
                };
            })
            .Where(x => x != null)
            .Cast<NavmapBaseListItem>()
            .ToArray();

        return new KnownNavmapBaseList(bases);
    }
}
