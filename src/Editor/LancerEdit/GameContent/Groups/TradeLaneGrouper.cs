using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;

namespace LancerEdit.GameContent.Groups;

public class TradeLaneGrouper
    : SystemObjectGrouper<GameObject, TradeLaneGroup>
{
    protected override bool IsGroupable(GameObject obj)
    {
        var arch = obj.SystemObject.Archetype?.Nickname;
        return arch != null &&
               arch.Contains("trade_lane", StringComparison.OrdinalIgnoreCase) &&
               obj.SystemObject.Dock.Kind == DockKinds.Tradelane;
    }

    protected override string GetKey(GameObject obj)
        => obj.Nickname;

    protected override IEnumerable<GameObject> GetLinkedItems(
        GameObject obj,
        IReadOnlyDictionary<string, GameObject> all)
    {
        var dock = obj.SystemObject.Dock;
        if (dock == null || dock.Kind != DockKinds.Tradelane)
            yield break;

        if (TryFollow(dock.Target, all, out var t1))
            yield return t1;

        if (TryFollow(dock.TargetLeft, all, out var t2))
            yield return t2;
    }

    private static bool TryFollow(
        string target,
        IReadOnlyDictionary<string, GameObject> all,
        out GameObject obj)
    {
        obj = null;

        if (string.IsNullOrEmpty(target))
            return false;

        if (!target.Contains("trade_lane", StringComparison.OrdinalIgnoreCase))
            return false;

        return all.TryGetValue(target, out obj);
    }
}
