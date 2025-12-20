// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
    [ParsedIni]
    public partial class StarSystem : UniverseElement
    {
        public bool MultiUniverse { get; private set; }

        [Entry("pos")]
        public Vector2? Pos;
        [Entry("msg_id_prefix")]
        public string MsgIdPrefix;
        [Entry("visit")]
        public int Visit;
        [Entry("ids_info")]
        public int IdsInfo;
        [Entry("navmapscale")]
        public float NavMapScale = 1;
        [Entry("file")]
        public string File;

        [Section("SystemInfo")]
        public SystemInfo Info;
        [Section("Archetype")]
        public List<SystemPreloads> Preloads = new();
        [Section("Background")]
        public SystemBackground Background;
        [Section("Ambient")]
        public SystemAmbient Ambient;
        [Section("Dust")]
        public SystemDust Dust;
        [Section("Music")]
        public SystemMusic Music;


        [Section("nebula")]
        public List<Nebula> Nebulae = new List<Nebula>();
        [Section("asteroids")]
        public List<AsteroidField> Asteroids = new List<AsteroidField>();
        [Section("lightsource")]
        public List<LightSource> LightSources = new List<LightSource>();
        [Section("object")]
        public List<SystemObject> Objects = new List<SystemObject>();
        [Section("encounterparameters")]
        public List<EncounterParameter> EncounterParameters = new List<EncounterParameter>();
        [Section("texturepanels")]
        public TexturePanelsRef TexturePanels;
        [Section("zone")]
        public List<Zone> Zones = new List<Zone>();


        [OnParseDependent]
        void ParseDependent(IniStringPool stringPool, IniParseProperties properties)
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
}
