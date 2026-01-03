using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public static class TradeLaneGrouper
{
    public static List<TradeLaneGroup> Build(IEnumerable<GameObject> items)
    {
        var all = items
            .Where(IsTradeLane)
            .ToDictionary(GetKey);

        var visited = new HashSet<string>();
        var groups = new List<TradeLaneGroup>();

        foreach (var item in all.Values)
        {
            var key = GetKey(item);
            if (visited.Contains(key))
                continue;

            var group = new TradeLaneGroup();
            Expand(item, all, visited, group);

            if (group.Members.Count > 0)
                groups.Add(group);
        }

        return groups;
    }
    public static bool IsTradeLane(GameObject obj)
    {
        var arch = obj.SystemObject.Archetype;
        return arch != null &&
               arch.Type == ArchetypeType.tradelane_ring && 
               obj.SystemObject?.Dock?.Kind == DockKinds.Tradelane;
    }

    static string GetKey(GameObject obj)
        => obj.Nickname;
    static bool TryFollow(string target,IReadOnlyDictionary<string, GameObject> all,out GameObject obj)
    {
        obj = null;

        if (string.IsNullOrEmpty(target))
            return false;

        if (!target.Contains("trade_lane", StringComparison.OrdinalIgnoreCase))
            return false;

        return all.TryGetValue(target, out obj);
    }
    static IEnumerable<GameObject> GetLinkedItems(GameObject obj,IReadOnlyDictionary<string, GameObject> all)
    {
        var dock = obj.SystemObject.Dock;
        if (dock == null || dock.Kind != DockKinds.Tradelane)
            yield break;

        if (TryFollow(dock.Target, all, out var t1))
            yield return t1;

        if (TryFollow(dock.TargetLeft, all, out var t2))
            yield return t2;
    }
    static void Expand(GameObject start,Dictionary<string, GameObject> all,HashSet<string> visited,TradeLaneGroup group)
    {
        var stack = new Stack<GameObject>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var item = stack.Pop();
            var key = GetKey(item);

            if (!visited.Add(key))
                continue;

            group.Add(item);

            foreach (var linked in GetLinkedItems(item, all))
                stack.Push(linked);
        }
    }
}
