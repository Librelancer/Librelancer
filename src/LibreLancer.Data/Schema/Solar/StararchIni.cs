// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Solar;

[ParsedIni]
public partial class StararchIni
{
    [Section("star")]
    public List<Star> Stars = [];
    [Section("star_glow")]
    public List<StarGlow> StarGlows = [];
    [Section("lens_flare")]
    public List<LensFlare> LensFlares = [];
    [Section("lens_glow")]
    public List<LensGlow> LensGlows = [];
    [Section("spines")]
    public List<Spines> Spines = [];
    [Section("texture")]
    public List<TextureSection> TextureFiles = [];

    public StararchIni(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        ParseIni(path, vfs, stringPool);
    }

}
