// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema;

public class DacomIni
{
    public MaterialMap? MaterialMap { get; private set; }
    public DacomIni (string dacomPath, FileSystem vfs)
    {
        foreach (Section s in IniFile.ParseFile(dacomPath, vfs, true)) {
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
                    break;
                default:
                    break;
            }

        }
    }
}
