// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedIni]
	public partial class StararchIni
	{
        [Section("star")]
        public List<Star> Stars = new List<Star>();
        [Section("star_glow")]
        public List<StarGlow> StarGlows = new List<StarGlow>();
        [Section("lens_flare")]
        public List<LensFlare> LensFlares = new List<LensFlare>();
        [Section("lens_glow")]
        public List<LensGlow> LensGlows = new List<LensGlow>();
        [Section("spines")]
        public List<Spines> Spines = new List<Spines>();
        [Section("texture")]
        public List<TextureSection> TextureFiles = new List<TextureSection>();

		public StararchIni(string path, FileSystem vfs, IniStringPool stringPool = null)
        {
            ParseIni(path, vfs, stringPool);
        }

	}
}
