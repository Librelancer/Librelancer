// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
namespace LibreLancer.Data.Audio
{
    public class AudioIni : IniFile
    {
        [Section("sound")]
        public List<AudioEntry> Entries = new List<AudioEntry>();
        public void AddIni(string path, FileSystem vfs)
        {
            ParseAndFill(path, vfs);
        }
    }
}
