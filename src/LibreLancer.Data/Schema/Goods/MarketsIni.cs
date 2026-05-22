// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Goods;

[ParsedIni]
public partial class MarketsIni
{
    [Section("basegood")]
    public List<BaseGood> BaseGoods = [];

    public void AddMarketsIni(string filename, FileSystem vfs, IniStringPool? stringPool = null)
    {
        foreach (var section in IniFile.ParseFile(filename, vfs, true, stringPool))
        {
            if (section.Name.Equals("BaseGood", StringComparison.OrdinalIgnoreCase))
            {
                if (BaseGood.TryParse(section, out var good, stringPool))
                {
                    good.SourceFile = filename;
                    BaseGoods.Add(good);
                }
            }
            else
            {
                IniDiagnostic.UnknownSection(section);
            }
        }
    }
}
