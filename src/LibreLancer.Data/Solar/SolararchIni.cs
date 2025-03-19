// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Solar
{
    [ParsedIni]
    public partial class SolararchIni
    {
        [Section("solar")]
        [Section("collisiongroup", Type = typeof(CollisionGroup), Child = true)]
        public List<Archetype> Solars = new();

        [Section("simple")]
        public List<Simple> Simples = new();

        public void AddSolararchIni(string path, FreelancerData gameData)
        {
            ParseIni(path, gameData.VFS);
        }
	}
}
