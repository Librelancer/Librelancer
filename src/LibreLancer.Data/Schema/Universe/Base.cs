// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
[ParsedIni]
public partial class Base : UniverseElement
{
    [Entry("system")]
    public string? System;

    [Entry("file", Required = true)]
    public string File = null!;

    [Entry("bgcs_base_run_by")]
    public string? BGCSBaseRunBy;
    [Entry("terrain_tiny")]
    public string? TerrainTiny;
    [Entry("terrain_sml")]
    public string? TerrainSml;
    [Entry("terrain_mdm")]
    public string? TerrainMdm;
    [Entry("terrain_lrg")]
    public string? TerrainLrg;
    [Entry("terrain_dyna_01")]
    public string? TerrainDyna1;
    [Entry("terrain_dyna_02")]
    public string? TerrainDyna2;
    [Entry("autosave_forbidden")]
    public bool? AutosaveForbidden;

    [Section("baseinfo")]
    public BaseInfo? BaseInfo;

    [Section("room")]
    public List<Room> Rooms = [];

    [OnParseDependent]
    private void ParseDependent(IniStringPool stringPool, IniParseProperties properties)
    {
        if (string.IsNullOrWhiteSpace(File)) return;
        if (properties["vfs"] is not FileSystem vfs) return;
        if (properties["dataPath"] is not string dataPath) return;
        ParseIni(dataPath + File, vfs, stringPool, properties);
    }
}
