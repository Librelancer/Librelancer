using System;
using System.Linq;
using System.Numerics;
using LancerEdit.GameContent.Groups;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;

namespace LancerEdit.GameContent.Groups;

public class TradeLaneGroup : SystemObjectGroup<GameObject>
{
    public Vector3 GetAveragePosition()
    {
        if (Members.Count == 0)
            return Vector3.Zero;

        Vector3 sum = Vector3.Zero;
        foreach (var m in Members)
            sum += m.LocalTransform.Position;

        return sum / Members.Count;
    }

    public (GameObject start, GameObject end) GetEndpoints()
    {
        var terminals = Members.Where(IsTerminal).ToList();
        if (terminals.Count >= 2)
            return (terminals[0], terminals[1]);

        // fallback: furthest apart
        GameObject a = null, b = null;
        float maxDist = 0;

        for (int i = 0; i < Members.Count; i++)
            for (int j = i + 1; j < Members.Count; j++)
            {
                float d = Vector3.DistanceSquared(
                    Members[i].LocalTransform.Position,
                    Members[j].LocalTransform.Position);
                if (d > maxDist)
                {
                    maxDist = d;
                    a = Members[i];
                    b = Members[j];
                }
            }

        return (a, b);
    }

    public bool IsTerminal(GameObject obj)
    {
        var dock = obj.SystemObject.Dock;
        if (dock == null) return true;
        if (dock.Kind != DockKinds.Tradelane) return true;

        bool left = IsTradeLaneName(dock.TargetLeft);
        bool right = IsTradeLaneName(dock.Target);

        return !(left && right);
    }

    private static bool IsTradeLaneName(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        return s.Contains("trade_lane", StringComparison.OrdinalIgnoreCase);
    }
}
