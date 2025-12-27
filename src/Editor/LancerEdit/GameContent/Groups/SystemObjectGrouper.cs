using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LancerEdit.GameContent.Groups;

namespace LancerEdit.GameContent.Groups;

public abstract class SystemObjectGrouper<TItem, TGroup>
    where TGroup : SystemObjectGroup<TItem>, new()
{
    public List<TGroup> Build(IEnumerable<TItem> items)
    {
        var all = items
            .Where(IsGroupable)
            .ToDictionary(GetKey);

        var visited = new HashSet<string>();
        var groups = new List<TGroup>();

        foreach (var item in all.Values)
        {
            var key = GetKey(item);
            if (visited.Contains(key))
                continue;

            var group = new TGroup();
            Expand(item, all, visited, group);

            if (group.Members.Count > 0)
                groups.Add(group);
        }

        return groups;
    }

    private void Expand(
        TItem start,
        Dictionary<string, TItem> all,
        HashSet<string> visited,
        TGroup group)
    {
        var stack = new Stack<TItem>();
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

    // ---- domain-specific hooks ----
    protected abstract bool IsGroupable(TItem item);
    protected abstract string GetKey(TItem item);

    protected abstract IEnumerable<TItem> GetLinkedItems(
        TItem item,
        IReadOnlyDictionary<string, TItem> all);
}


