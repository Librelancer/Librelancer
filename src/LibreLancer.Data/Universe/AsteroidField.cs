﻿// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Universe
{
    [ParsedSection]
    [ParsedIni]
	public partial class AsteroidField : ZoneReference
    {
        [Section("cube")]
        public AsteroidCube Cube;

        [Section("field")]
        public Field Field;

        [Section("band")]
        public Band Band;
        [Section("exclusionband")]
        public Band ExclusionBand;
        [Section("asteroidbillboards")]
        public AsteroidBillboards AsteroidBillboards;

        [Section("dynamicasteroids")]
		public List<DynamicAsteroids> DynamicAsteroids = new List<DynamicAsteroids>();

        [Section("lootablezone")]
		public List<LootableZone> LootableZones = new List<LootableZone>();

        [Section("exclusion zones", Delimiters = new[] { "exclude", "exclusion" })]
        public List<AsteroidExclusion> ExclusionZones = new List<AsteroidExclusion>();

        [OnParseDependent]
        void ParseDependent(IniParseProperties properties)
        {
            if (string.IsNullOrWhiteSpace(IniFile)) return;
            if (properties["vfs"] is not FileSystem vfs) return;
            if (properties["dataPath"] is not string dataPath) return;
            ParseIni(dataPath + IniFile, vfs);
        }
    }
}
