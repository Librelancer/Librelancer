// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Goods
{
    [ParsedIni]
    public partial class MarketsIni
    {
        [Section("basegood")]
        public List<BaseGood> BaseGoods = new List<BaseGood>();

        public void AddMarketsIni(string filename, FileSystem vfs, IniStringPool stringPool = null) => ParseIni(filename, vfs, stringPool);
    }
}
