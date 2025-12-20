// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Audio
{
    [ParsedIni]
    public partial class AudioIni
    {
        [Section("sound")]
        public List<AudioEntry> Entries = new List<AudioEntry>();
        public void AddIni(string path, FileSystem vfs, IniStringPool stringPool = null)
        {
            ParseIni(path, vfs,  stringPool);
        }
    }
}
