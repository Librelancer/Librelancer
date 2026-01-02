// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
[ParsedIni]
public partial class StarSystem : UniverseElement
{
    public bool MultiUniverse { get; private set; }

    [Entry("pos")]
    public Vector2? Pos;
    [Entry("msg_id_prefix")]
    public string? MsgIdPrefix;
    [Entry("visit")]
    public int Visit;
    [Entry("ids_info")]
    public int IdsInfo;
    [Entry("navmapscale")]
    public float NavMapScale = 1;
    [Entry("file", Required = true)]
    public string File = null!;

    [Section("SystemInfo")]
    public SystemInfo? Info;
    [Section("Archetype")]
    public List<SystemPreloads> Preloads = [];
    [Section("Background")]
    public SystemBackground? Background;
    [Section("Ambient")]
    public SystemAmbient? Ambient;
    [Section("Dust")]
    public SystemDust? Dust;
    [Section("Music")]
    public SystemMusic? Music;


    [Section("nebula")]
    public List<Nebula>? Nebulae = [];
    [Section("asteroids")]
    public List<AsteroidField>? Asteroids = [];
    [Section("lightsource")]
    public List<LightSource>? LightSources = [];
    [Section("object")]
    public List<SystemObject> Objects = [];
    [Section("encounterparameters")]
    public List<EncounterParameter> EncounterParameters = [];
    [Section("texturepanels")]
    public TexturePanelsRef? TexturePanels;
    [Section("zone")]
    public List<Zone>? Zones = [];


    [OnParseDependent]
    private void ParseDependent(IniStringPool stringPool, IniParseProperties properties)
    {
        if (string.IsNullOrWhiteSpace(File))
        {
            FLLog.Warning("Ini", "Unimplemented: Possible MultiUniverse system " + Nickname);
            MultiUniverse = true;
            return;
        }
        if (properties["vfs"] is not FileSystem vfs) return;
        if (properties["universePath"] is not string universePath) return;
        ParseIni(universePath + File, vfs, stringPool, properties);
    }
}
