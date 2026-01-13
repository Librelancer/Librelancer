using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public static class TradeLaneGrouper
{
    public static List<TradeLaneGroup> Build(IEnumerable<GameObject> items)
    {
        // Filter once to concrete tradelane rings
        var tradeLanes = items
            .Where(IsTradeLane)
            .ToList();

        // Start points: tradelanes with a forward target but no backward link
        var starts = tradeLanes.Where(t =>
        {
            var dock = t.SystemObject.Dock;
            return !string.IsNullOrEmpty(dock.Target) &&
                   string.IsNullOrEmpty(dock.TargetLeft);
        });

        var groups = new List<TradeLaneGroup>();

        foreach (var start in starts)
        {
            var group = new TradeLaneGroup();
            var current = start;

            while (current != null)
            {
                group.Add(current);

                var dock = current.SystemObject.Dock;
                if (string.IsNullOrEmpty(dock.Target))
                    break;

                current = tradeLanes.FirstOrDefault(t => t.Nickname == dock.Target);
            }

            if (group.Members.Count > 0)
                groups.Add(group);
        }

        return groups;
    }
    public static bool IsTradeLane(GameObject obj)
    {
        var arch = obj.SystemObject.Archetype;
        return arch is { Type: ArchetypeType.tradelane_ring } &&
               obj.SystemObject?.Dock?.Kind == DockKinds.Tradelane;
    }
}
