// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.MBases
{
    [ParsedIni]
	public partial class MBasesIni
    {
        [Section("mbase")]
        [Section("mvendor", Type = typeof(MVendor), Child = true)]
        [Section("mroom", Type = typeof(MRoom), Child = true)]
        [Section("gf_npc", Type = typeof(GfNpc), Child = true)]
        [Section("basefaction", Type = typeof(BaseFaction), Child = true)]
        public List<MBase> MBases = new();

        public void AddFile(string path, FileSystem vfs, IniStringPool stringPool = null)
        {
            ParseIni(path, vfs, stringPool);
        }
	}
}
