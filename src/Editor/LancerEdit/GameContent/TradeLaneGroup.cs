using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public class TradeLaneGroup
{
    public readonly List<GameObject> Members = new();

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

    private bool IsTerminal(GameObject obj)
    {
        var dock = obj.SystemObject.Dock;
        if (dock == null) return true;
        if (dock.Kind != DockKinds.Tradelane) return true;

        bool left = string.IsNullOrWhiteSpace(dock.TargetLeft);
        bool right = string.IsNullOrWhiteSpace(dock.Target);

        return left && right;
    }
    public void Add(GameObject obj)
    {
        if (!Members.Contains(obj))
            Members.Add(obj);
    }
}
