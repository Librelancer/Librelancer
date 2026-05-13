// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema;

public class DacomIni
{
    public MaterialMap? MaterialMap { get; private set; }
    public bool HasMultiUniverse { get; private set; }

    public DacomIni (string dacomPath, FileSystem vfs)
    {
        foreach (Section s in IniFile.ParseFile(dacomPath, vfs, true)) {
            HasMultiUniverse |= SectionReferencesMultiUniverse(s);

            switch (s.Name.ToLowerInvariant ()) {
                case "materialmap":
                    var map = new MaterialMap ();
                    foreach (Entry e in s) {
                        if (e.Name.ToLowerInvariant () != "name") {
                            map.AddMap (e.Name, e [0].ToString());
                        } else {
                            map.AddRegex (e [0].ToKeyValue ());
                        }
                    }
                    MaterialMap = map;
                    break;
                default:
                    break;
            }

        }
    }

    private static bool SectionReferencesMultiUniverse(Section section)
    {
        foreach (Entry e in section) {
            if (ContainsMultiUniverse(e.Name))
                return true;
            for (var i = 0; i < e.Count; i++) {
                if (ContainsMultiUniverse(e[i].ToString()))
                    return true;
            }
        }
        return false;
    }

    private static bool ContainsMultiUniverse(string value) =>
        value.Contains("multiuniverse.dll", StringComparison.OrdinalIgnoreCase);
}
