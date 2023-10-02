using System.Collections.Generic;

namespace LibreLancer.Utf.Anm;

public class StringDeduplication
{
    private Dictionary<string, string> strings = new Dictionary<string, string>();

    public string Get(string s)
    {
        if (strings.TryGetValue(s, out var d))
            return d;
        strings[s] = s;
        return s;
    }
}
