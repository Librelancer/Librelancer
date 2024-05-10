using LibreLancer.Ini;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    private static IEnumerable<Section> ParseFile(string file, bool preparse = true, bool allowmaps = false)
    {
        var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
        var parser = new LancerTextIniParser();
        return parser.ParseIniFile(file, stream, preparse, allowmaps);
    }

    private static IEnumerable<Section> ParseFile(Stream stream, bool preparse = true, bool allowmaps = false)
    {
        var parser = new LancerTextIniParser();
        return parser.ParseIniFile(null, stream, preparse, allowmaps);
    }
}

