namespace LLShaderCompiler;

using System.Collections.Generic;
using System.Linq;

public static class FeatureHelper
{
    public static IEnumerable<uint> AllPermutations(IEnumerable<uint> caps) =>
        CapsPermute(caps).Order();

    private static IEnumerable<uint> CapsPermute(IEnumerable<uint> caps)
    {
        yield return 0;
        var vals = caps.Select(x => x).ToArray();
        var valsinv = vals.Select(v => ~v).ToArray();
        var max = 0U;
        for (uint i = 0; i < vals.Length; i++) max |= vals[i];
        for (uint i = 0; i <= max; i++)
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
