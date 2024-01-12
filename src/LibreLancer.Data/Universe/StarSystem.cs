// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
    [SelfSection("systeminfo")]
    [SelfSection("music")]
    [SelfSection("dust")]
    [SelfSection("ambient")]
    [SelfSection("background")]
    [SelfSection("archetype")]
    public class StarSystem : UniverseElement
    {
        public bool MultiUniverse { get; private set; }

        //TODO: Entry should clarify which self section it's in
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
        string systemFile;
        [Entry("space_color")]
        public Color4 SpaceColor = Color4.Black;
        [Entry("local_faction")]
        public string LocalFaction;
        [Entry("rpop_solar_detection")]
        public bool RpopSolarDetection;
        [Entry("space_farclip")]
        public float? SpaceFarClip;
        [Entry("space")]
        public string MusicSpace;
        [Entry("danger")]
        public string MusicDanger;
        [Entry("battle")]
        public string MusicBattle;
        [Entry("ship", Multiline = true)]
        public List<string> ArchetypeShip = new List<string>();
        [Entry("simple", Multiline =  true)]
        public List<string> ArchetypeSimple = new List<string>();
        [Entry("solar", Multiline = true)]
        public List<string> ArchetypeSolar = new List<string>();
        [Entry("equipment", Multiline = true)]
        public List<string> ArchetypeEquipment = new List<string>();
        [Entry("snd", Multiline = true)]
        public List<string> ArchetypeSnd = new List<string>();
        [Entry("voice", Multiline = true)]
        public List<string[]> ArchetypeVoice = new List<string[]>();
        [Entry("basic_stars")]
        public string BackgroundBasicStarsPath;
        [Entry("complex_stars")]
        public string BackgroundComplexStarsPath;
        [Entry("nebulae")]
        public string BackgroundNebulaePath;
        [Entry("dust")]
        [Entry("spacedust")]
        public string Spacedust;
        [Entry("spacedust_maxparticles")]
        public int SpacedustMaxParticles;
        [Entry("color")]
        public Color4 AmbientColor = Color4.Black;

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
        //[Section("field")]
        //public Field Field;
        //[Section("asteroidbillboards")]
        //public AsteroidBillboards AsteroidBillboards;

        public string SourceFile { get; private set; }


        public StarSystem(string universePath, Section section, FreelancerData data)
            : base(data)
        {
            if (section == null) throw new ArgumentNullException("section");
            SelfFromSection(section);

            if (systemFile == null) { //TODO: MultiUniverse
                FLLog.Warning("Ini", "Unimplemented: Possible MultiUniverse system " + Nickname);
                MultiUniverse = true;
                return;
            }

            SourceFile = systemFile;

            ParseAndFill(universePath + systemFile, data.Freelancer.DataPath, data.VFS);
        }
    }
}
