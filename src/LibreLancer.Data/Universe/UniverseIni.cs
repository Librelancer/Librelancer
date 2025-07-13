// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Universe
{
    [ParsedIni]
    [IgnoreSection("time")]
	public partial class UniverseIni
    {
        [Section("base")]
        public List<Base> Bases = new();
        [Section("system")]
        public List<StarSystem> Systems = new();

        public UniverseIni() { }

		public UniverseIni(string path, FreelancerData data)
        {
            var props = new IniParseProperties([
                new("dataPath", data.Freelancer.DataPath),
                new ("universePath", data.VFS.RemovePathComponent(path)),
                new("vfs", data.VFS)
            ]);
            ParseIni(path, data.VFS, props);
        }
    }
}
