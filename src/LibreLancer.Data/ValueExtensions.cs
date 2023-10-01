using System.Collections.Generic;
using System.Globalization;

namespace LibreLancer.Data;

public static class ValueExtensions
{
    public static string ToStringInvariant(this float f) =>
        f == 0 ? "0" :  f.ToString("0.############", CultureInfo.InvariantCulture);

    public static IEnumerable<string> SplitInGroups(this string original, int size)
    {
        var p = 0;
        var l = original.Length;
        while (l - p > size)
        {
            yield return original.Substring(p, size);
            p += size;
        }
        var s = original.Substring(p);
        if (!string.IsNullOrWhiteSpace(s) && !string.IsNullOrEmpty(s)) yield return s;
    }
}
