// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;

namespace ShaderProcessor;

public static class FeatureHelper
{
    public static IEnumerable<string[]> Permute(string type, string[] s)
    {
        var enumbits = new List<int>();
        for (var i = 0; i < s.Length; i++)
            enumbits.Add(1 << i);
        foreach (var c in IntPermute(enumbits))
        {
            var strs = new List<string>();
            for (var i = 0; i < s.Length; i++)
            {
                var flag = 1 << i;
                if ((c & flag) == flag)
                    strs.Add(s[i]);
            }

            if (!string.IsNullOrWhiteSpace(type))
                yield return strs.Select(x => $"{type}.{x}").ToArray();
            else
                yield return strs.ToArray();
        }
    }

    private static IEnumerable<int> IntPermute(List<int> caps)
    {
        if (caps == null || caps.Count == 0) yield break;
        var vals = caps.Select(x => x).ToArray();
        var valsinv = vals.Select(v => ~v).ToArray();
        var max = 0;
        for (var i = 0; i < vals.Length; i++) max |= vals[i];
        for (var i = 0; i <= max; i++)
        {
            var unaccountedBits = i;
            for (var j = 0; j < valsinv.Length; j++)
            {
                unaccountedBits &= valsinv[j];
                if (unaccountedBits == 0)
                {
                    if (i != 0)
                        yield return i;
                    break;
                }
            }
        }
    }
}