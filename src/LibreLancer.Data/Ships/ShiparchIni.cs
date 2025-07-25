// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Solar;

namespace LibreLancer.Data.Ships
{
    [ParsedIni]
	public partial class ShiparchIni
    {
        [Section("ship")]
        [Section("collisiongroup", Type = typeof(CollisionGroup), Child = true)]
        public List<Ship> Ships = new();

        [Section("simple")]
        public List<Simple> Simples = new();

        public void ParseAllInis(IEnumerable<string> paths, FreelancerData fldata)
		{
            ParseInis(paths, fldata.VFS);
        }
    }
}

