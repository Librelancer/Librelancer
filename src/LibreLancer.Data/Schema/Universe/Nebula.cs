// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Universe;

[ParsedIni]
[ParsedSection]
public partial class Nebula : ZoneReference
{
    [Section("fog")]
    public NebulaFog? Fog;

    [Section("exterior")]
    public NebulaExterior? Exterior;

    [Section("nebulalight")]
    public List<NebulaLight> NebulaLights = [];

    [Section("clouds")]
    public List<NebulaClouds> Clouds = [];

    [Section("backgroundlightning")]
    public NebulaBackgroundLightning? BackgroundLightning;

    [Section("dynamiclightning")]
    public NebulaDynamicLightning? DynamicLightning;

    [Section("exclusion zones", Delimiters = ["exclude", "exclusion"])]
    public List<NebulaExclusion> ExclusionZones = [];

    [OnParseDependent]
    private void ParseDependent(IniStringPool stringPool, IniParseProperties properties)
    {
        if (string.IsNullOrWhiteSpace(IniFile)) return;
        if (properties["vfs"] is not FileSystem vfs) return;
        if (properties["dataPath"] is not string dataPath) return;
        ParseIni(dataPath + IniFile, vfs, stringPool, properties);
    }
}
