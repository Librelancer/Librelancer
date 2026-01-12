using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;

namespace LibreLancer.Data.GameData;

public static class ShortestPaths
{
    [Flags]
    private enum ConnectionKind
    {
        Legal = (1 << 0),
        Illegal = (1 << 1)
    }

    private record Connection(ConnectionKind Kind, SystemPaths Target);

    private class SystemPaths(StarSystem system)
    {
        public readonly StarSystem System = system;
        public readonly HashSet<Connection> Connections = [];
        public SystemPaths? Parent;

        public List<StarSystem> GetPath()
        {
            var ls = new List<StarSystem>();
            SystemPaths? p = this;

            do
            {
                ls.Add(p.System);
                p = p.Parent;
            } while (p != null);

            ls.Reverse();
            return ls;
        }
    }

    public static void CalculateShortestPaths(GameItemDb gameData)
    {
        var connections = BuildConnections(gameData);
        foreach (var c in connections)
        {
            c.System.ShortestPathsLegal = CalculateSystem(c, ConnectionKind.Legal);
            c.System.ShortestPathsIllegal = CalculateSystem(c, ConnectionKind.Illegal);
            c.System.ShortestPathsAny = CalculateSystem(c, ConnectionKind.Legal | ConnectionKind.Illegal);
        }
    }


    private static Dictionary<StarSystem, List<StarSystem>> CalculateSystem(SystemPaths src, ConnectionKind kind)
    {
        var q = new Queue<SystemPaths>();
        var visited = new HashSet<SystemPaths>();
        src.Parent = null;
        q.Enqueue(src);
        Dictionary<StarSystem, List<StarSystem>> paths = new();
        while (q.Count > 0)
        {
            var system = q.Dequeue();
            paths[system.System] = system.GetPath();
            visited.Add(system);
            foreach (var c in system.Connections
                         .Where(x => (x.Kind & kind) != 0)
                         .Select(x => x.Target)
                         .Distinct())
            {
                if (visited.Contains(c))
                    continue;
                c.Parent = system;
                q.Enqueue(c);
                visited.Add(c);
            }
        }
        return paths;
    }

    private static SystemPaths[] BuildConnections(GameItemDb gameData)
    {
        var all = gameData.Systems.Select(x => new SystemPaths(x)).ToArray();
        var lookups = new Dictionary<string, SystemPaths>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in all)
            lookups[a.System.Nickname] = a;

        foreach (var sys in all) {
            foreach (var obj in sys.System.Objects)
            {
                if (obj.Dock?.Kind != DockKinds.Jump ||
                    (!(!obj.Dock.Target?.Equals(sys.System.Nickname, StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    continue;
                }

                var kind = obj.Archetype?.Type == ArchetypeType.jump_gate
                    ? ConnectionKind.Legal
                    : ConnectionKind.Illegal;

                if (!lookups.TryGetValue(obj.Dock.Target, out var tgt))
                    continue;
                sys.Connections.Add(new(kind, tgt));
            }
        }

        return all;
    }
}
