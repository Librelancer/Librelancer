using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer.Server.RandomMissions;

public class VC6Random(int seed)
{
    int next = seed;

    public int Next()
    {
        next = next * 214013 + 2531011;
        return (next >> 16) & 0x7fff;
    }

    public int NextInt(int min, int max)
    {
        int range = max - min + 1;
        return min + ((Next() * range) >> 15);
    }

    public T Select<T>(IReadOnlyList<T> items, Func<T, float> weight)
    {
        var probabilitySum = items.Sum(weight);
        var selected = (Next() / 32767f) * probabilitySum;
        T v = items[0];
        float cProb = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var w = weight(items[i]);
            if (selected < cProb + w)
            {
                v = items[i];
                break;
            }
            cProb += w;
        }
        return v;
    }

}
