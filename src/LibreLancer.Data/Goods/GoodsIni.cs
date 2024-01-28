// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
namespace LibreLancer.Data.Goods
{
    public class GoodsIni : IniFile
    {
        [Section("good")]
        public List<Good> Goods = new List<Good>();

        public void AddGoodsIni(string filename, FileSystem vfs) => ParseAndFill(filename, vfs);
    }
}
