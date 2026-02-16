// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Fonts;

public class RichFontsIni
{
    public List<RichFont> Fonts = [];
    public void AddRichFontsIni(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        foreach (var section in IniFile.ParseFile(path, vfs, false, stringPool))
        {
            if (section.Name.ToLowerInvariant() != "truetype")
            {
                continue;
            }

            foreach (var e in section.Where(e => e.Name.ToLowerInvariant() == "font"))
            {
                Fonts.Add(new RichFont() { Index = e[0].ToInt32(), Name = e[1].ToString(), Size = e[2].ToInt32() });
            }
        }
    }
}
