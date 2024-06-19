// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LibreLancer.Data.Equipment;
using LibreLancer.Data.Solar;
using LibreLancer.Data.Characters;
using LibreLancer.Data.Universe;
using LibreLancer.Data.Ships;
using LibreLancer.Data.Audio;
using LibreLancer.Data.Cameras;
using LibreLancer.Data.Effects;
using LibreLancer.Data.Goods;
using LibreLancer.Data.Fuses;
using LibreLancer.Data.InitialWorld;
using LibreLancer.Data.Interface;
using LibreLancer.Data.IO;
using LibreLancer.Data.Missions;
using LibreLancer.Data.NewCharDB;
using LibreLancer.Data.Pilots;
using LibreLancer.Data.Storyline;
using LibreLancer.Data.Voices;

namespace LibreLancer.Data
{
    public class FreelancerData
    {
        //Config
        public DacomIni Dacom;
        public FreelancerIni Freelancer;
        public FileSystem VFS;
        //Data
        public CameraIni Cameras;
        public InfocardManager Infocards;
        public EffectsIni Effects;
        public ExplosionsIni Explosions;
        public FuseIni Fuses;
        public EquipmentIni Equipment;
        public HpTypesIni HpTypes;
        public LoadoutsIni Loadouts;
        public SolararchIni Solar;
        public StararchIni Stars;
        public BodypartsIni Bodyparts;
        public CostumesIni Costumes;
        public UniverseIni Universe;
        public ShiparchIni Ships;
        public AudioIni Audio;
        public GoodsIni Goods;
        public MarketsIni Markets;
        public GraphIni Graphs;
        public TexturePanels EffectShapes;
        public MouseIni Mouse;
        public AsteroidArchIni Asteroids;
        public FontsIni Fonts;
        public RichFontsIni RichFonts;
        public NewsIni News;
        public PetalDbIni PetalDb;
        public HudIni Hud;
        public BaseNavBarIni BaseNavBar;
        public MBasesIni MBases;
        public NewCharDBIni NewCharDB;
        public ContentDll ContentDll;
        public InfocardMapIni InfocardMap;
        public InitialWorldIni InitialWorld;
        public FactionPropIni FactionProps;
        public FormationsIni Formations;
        public EmpathyIni Empathy;
        public NavmapIni Navmap; //Extension
        public NPCShipIni NPCShips;
        public PilotsIni Pilots;
        public StateGraphDb StateGraphDb;
        public KeymapIni Keymap;
        public KeyListIni KeyList;
        public VoicesIni Voices;
        public StorylineIni Storyline;
        public string DataVersion;
        public bool Loaded = false;

        public bool LoadDacom = true;

        public SpecificNPCIni SpecificNPCs;

        public MissionIni LoadMissionIni(StoryMission item)
        {
            if (VFS.FileExists(Freelancer.DataPath + item.File))
            {
                var m = new MissionIni(Freelancer.DataPath + item.File, VFS);
                if (m.Info?.NpcShipFile != null)
                {
                    m.ShipIni = new NPCShipIni(Freelancer.DataPath + m.Info.NpcShipFile, VFS);
                }
                return m;
            }
            return null;
        }


        public FreelancerData (FreelancerIni fli, FileSystem vfs)
        {
            Freelancer = fli;
            VFS = vfs;
        }

        public void LoadData()
        {
            if (Loaded)
                return;
            if (LoadDacom)
            {
                if (!string.IsNullOrEmpty(Freelancer.DacomPath)) {
                    Dacom = new DacomIni(Freelancer.DacomPath, VFS);
                }
                else {
                    new MaterialMap(); //no dacom, make default global thing
                    //todo: fix this
                }
            }

            Infocards = new InfocardManager(Freelancer.Resources);

            List<Task> tasks = new List<Task>();

            void Run(Action a) => tasks.Add(Task.Run(a));

            Run(() =>
            {
                Equipment = new EquipmentIni();
                Equipment.ParseAllInis(Freelancer.EquipmentPaths, this);
            });
            Run(() =>
            {
                Solar = new SolararchIni();
                foreach (var file in Freelancer.SolarPaths) {
                    Solar.AddSolararchIni(file, this);
                }
                if (Freelancer.StarsPath != null)
                    Stars = new StararchIni(Freelancer.StarsPath, VFS);
                else
                    Stars = new StararchIni(Freelancer.DataPath + "SOLAR\\stararch.ini", VFS);
            });
            Run(() =>
            {
                Asteroids = new AsteroidArchIni();
                foreach (var ast in Freelancer.AsteroidPaths)
                    Asteroids.AddFile(ast, VFS);
            });
            Run(() =>
            {
                SpecificNPCs = new SpecificNPCIni();
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\specific_npc.ini"))
                {
                    SpecificNPCs.AddFile(Freelancer.DataPath + "MISSIONS\\specific_npc.ini", VFS);
                }
            });
            Run(() =>
            {
                Loadouts = new LoadoutsIni();
                foreach (var lo in Freelancer.LoadoutPaths)
                    Loadouts.AddLoadoutsIni(lo, this);
            });
            Run(() =>
            {
                Universe = new UniverseIni(Freelancer.UniversePath, this);
            });
            //Pilots
            Run(() =>
            {
                Pilots = new PilotsIni();
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\pilots_population.ini"))
                {
                    Pilots.AddFile(Freelancer.DataPath + "MISSIONS\\pilots_population.ini", VFS);
                }
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\pilots_story.ini"))
                {
                    Pilots.AddFile(Freelancer.DataPath + "MISSIONS\\pilots_story.ini", VFS);
                }
            });
            //Graphs
            Run(() =>
            {
                Graphs = new GraphIni();
                foreach (var g in Freelancer.GraphPaths)
                    Graphs.AddGraphIni(g, VFS);
            });
            //Shapes
            Run(() =>
            {
                if (string.IsNullOrEmpty(Freelancer.EffectShapesPath))
                    throw new Exception("Need one effect_shapes entry");
                EffectShapes = new TexturePanels(Freelancer.EffectShapesPath, VFS);
            });
            //Effects
            Run(() =>
            {
                Effects = new EffectsIni();
                foreach (var fx in Freelancer.EffectPaths)
                    Effects.AddIni(fx, VFS);
            });
            Run(() =>
            {
                Explosions = new ExplosionsIni();
                foreach(var fx in Freelancer.ExplosionPaths)
                    Explosions.AddFile(fx, VFS);
            });
            Run(() =>
            {
                //Mouse
                Mouse = new MouseIni(Freelancer.MousePath, VFS);
                //Fonts
                RichFonts = new RichFontsIni();
                foreach (var rf in Freelancer.RichFontPaths)
                    RichFonts.AddRichFontsIni(rf, VFS);
                Fonts = new FontsIni();
                foreach (var f in Freelancer.FontPaths)
                    Fonts.AddFontsIni(f, VFS);
            });
            Run(() =>
            {
                //PetalDb
                PetalDb = new PetalDbIni();
                foreach (var pt in Freelancer.PetalDbPaths)
                    PetalDb.AddFile(pt, VFS);
            });
            Run(() =>
            {
                //Hud
                Hud = new HudIni();
                if (string.IsNullOrEmpty(Freelancer.HudPath)) throw new Exception("Need one hud path");
                Hud.AddIni(Freelancer.HudPath, VFS);
                //navbar.ini
                BaseNavBar = new BaseNavBarIni(Freelancer.DataPath, VFS);
                if (!string.IsNullOrEmpty(Freelancer.NavmapPath)) Navmap = new NavmapIni(Freelancer.NavmapPath, VFS);
            });
            Run(() =>
            {
                InfocardMap = new InfocardMapIni();
                InfocardMap.AddMap(Freelancer.DataPath + "/INTERFACE/infocardmap.ini", VFS);
            });
            Run(() =>
            {
                //mbases.ini
                MBases = new MBasesIni();
                if (Freelancer.MBasesPaths != null)
                {
                    foreach(var f in Freelancer.MBasesPaths)
                        MBases.AddFile(f, VFS);
                }
                else
                {
                    MBases.AddFile(Freelancer.DataPath + "MISSIONS\\mbases.ini", VFS);
                }
            });
            Run(() =>
            {
                Formations = new FormationsIni();
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\formations.ini"))
                {
                    Formations.AddFile(Freelancer.DataPath + "MISSIONS\\formations.ini", VFS);
                }
            });
            Run(() =>
            {
                Voices = new VoicesIni();
                foreach(var voice in Freelancer.VoicePaths)
                    Voices.AddVoicesIni(voice, VFS);
            });
            Run(() =>
            {
                News = new NewsIni();
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\news.ini"))
                {
                    News.AddNewsIni(Freelancer.DataPath + "MISSIONS\\news.ini", VFS);
                }
            });
            Run(() =>
            {
                Fuses = new FuseIni();
                foreach (var fi in Freelancer.FusePaths)
                    Fuses.AddFuseIni(fi, VFS);
            });
            //newchardb
            Run(() =>
            {
                NewCharDB = new NewCharDBIni();
                foreach (var nc in Freelancer.NewCharDBPaths)
                    NewCharDB.AddNewCharDBIni(nc, VFS);
            });
            Run(() =>
            {
                Bodyparts = new BodypartsIni(Freelancer.BodypartsPath, this);
                Costumes = new CostumesIni(Freelancer.CostumesPath, this);
            });
            Run(() =>
            {
                Audio = new AudioIni();
                foreach (var snd in Freelancer.SoundPaths)
                    Audio.AddIni(snd, VFS);
            });
            Run(() =>
            {
                Ships = new ShiparchIni();
                Ships.ParseAllInis(Freelancer.ShiparchPaths, this);
            });
            Run(() =>
            {
                Goods = new GoodsIni();
                foreach (var gd in Freelancer.GoodsPaths)
                    Goods.AddGoodsIni(gd, VFS);
            });
            Run(() =>
            {
                Markets = new MarketsIni();
                foreach (var mkt in Freelancer.MarketsPaths)
                    Markets.AddMarketsIni(mkt, VFS);
            });
            Run(() =>
            {
                if (VFS.FileExists(Freelancer.DataPath + "missions\\npcships.ini"))
                    NPCShips = new NPCShipIni(Freelancer.DataPath + "missions\\npcships.ini", VFS);
            });
            Run(() =>
            {
                Cameras = new CameraIni(Freelancer.CamerasPath, VFS);
            });
            Run(() =>
            {
                HpTypes = new HpTypesIni();
                HpTypes.LoadDefault();
            });
            Run(() =>
            {
                Keymap = new KeymapIni(Freelancer.DataPath + "interface\\keymap.ini", VFS);
                KeyList = new KeyListIni(Freelancer.DataPath + "interface\\keylist.ini", VFS);
            });
            Run(() =>
            {
                InitialWorld = new InitialWorldIni();
                InitialWorld.AddFile(Freelancer.DataPath + "initialworld.ini", VFS);
            });
            Run(() =>
            {
                FactionProps = new FactionPropIni();
                FactionProps.AddFile(Freelancer.DataPath + "missions\\faction_prop.ini", VFS);
            });
            Run(() =>
            {
                Empathy = new EmpathyIni();
                Empathy.AddFile(Freelancer.DataPath + "missions\\empathy.ini", VFS);
            });
            Run(() =>
            {
                StateGraphDb = new StateGraphDb(Freelancer.DataPath + "AI\\state_graph.db", VFS);
            });
            Run(() =>
            {
                Storyline = new StorylineIni();
                Storyline.AddDefault();
            });
            ContentDll = new ContentDll();
            if (VFS.FileExists("DLLS\\BIN\\content.dll"))
                ContentDll.Load(VFS.ReadAllBytes("DLLS\\BIN\\content.dll"));
            if (!string.IsNullOrEmpty(Freelancer.DataVersion))
                DataVersion = Freelancer.DataVersion;
            else
                DataVersion = "FL-1";
            Task.WaitAll(tasks.ToArray());
            Loaded = true;
        }
    }
}
