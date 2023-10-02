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
using LibreLancer.Data.Missions;
using LibreLancer.Data.NewCharDB;
using LibreLancer.Data.Pilots;

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

        public string DataVersion;
        public bool Loaded = false;

        public bool LoadDacom = true;

        static readonly string[] missionFiles = new string[]
        {
            "MISSIONS\\M01A\\m01a.ini",
            "MISSIONS\\M01B\\m01b.ini",
            "MISSIONS\\M02\\m02.ini",
            "MISSIONS\\M03\\m03.ini",
            "MISSIONS\\M04\\m04.ini",
            "MISSIONS\\M05\\m05.ini",
            "MISSIONS\\M06\\m06.ini",
            "MISSIONS\\M07\\m07.ini",
            "MISSIONS\\M08\\m08.ini",
            "MISSIONS\\M09\\m09.ini",
            "MISSIONS\\M10\\m10.ini",
            "MISSIONS\\M11\\M11.ini",
            "MISSIONS\\M12\\M12.ini",
            "MISSIONS\\M13\\M13.ini"
        };

        public int MissionCount => missionFiles.Length;

        public SpecificNPCIni SpecificNPCs;

        public MissionIni LoadMissionIni(int index)
        {
            var msn = missionFiles[index];
            if (VFS.FileExists(Freelancer.DataPath + msn))
            {
                var m = new MissionIni(Freelancer.DataPath + msn, VFS);
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
            tasks.Add(Task.Run(() =>
            {
                Equipment = new EquipmentIni();
                Equipment.ParseAllInis(Freelancer.EquipmentPaths, this);
            }));
            tasks.Add(Task.Run(() =>
            {
                Solar = new SolararchIni(Freelancer.SolarPath, this);
                if (Freelancer.StarsPath != null)
                    Stars = new StararchIni(Freelancer.StarsPath, VFS);
                else
                    Stars = new StararchIni(Freelancer.DataPath + "SOLAR\\stararch.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Asteroids = new AsteroidArchIni();
                foreach (var ast in Freelancer.AsteroidPaths)
                    Asteroids.AddFile(ast, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                SpecificNPCs = new SpecificNPCIni();
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\specific_npc.ini"))
                {
                    SpecificNPCs.AddFile(Freelancer.DataPath + "MISSIONS\\specific_npc.ini", VFS);
                }
            }));
            tasks.Add(Task.Run(() =>
            {
                Loadouts = new LoadoutsIni();
                foreach (var lo in Freelancer.LoadoutPaths)
                    Loadouts.AddLoadoutsIni(lo, this);
            }));
            tasks.Add(Task.Run(() =>
            {
                Universe = new UniverseIni(Freelancer.UniversePath, this);
            }));
            //Pilots
            tasks.Add(Task.Run(() =>
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
            }));
            //Graphs
            tasks.Add(Task.Run(() =>
            {
                Graphs = new GraphIni();
                foreach (var g in Freelancer.GraphPaths)
                    Graphs.AddGraphIni(g, VFS);
            }));
            //Shapes
            tasks.Add(Task.Run(() =>
            {
                if (string.IsNullOrEmpty(Freelancer.EffectShapesPath))
                    throw new Exception("Need one effect_shapes entry");
                EffectShapes = new TexturePanels(Freelancer.EffectShapesPath, VFS);
            }));
            //Effects
            tasks.Add(Task.Run(() =>
            {
                Effects = new EffectsIni();
                foreach (var fx in Freelancer.EffectPaths)
                    Effects.AddIni(fx, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Explosions = new ExplosionsIni();
                foreach(var fx in Freelancer.ExplosionPaths)
                    Explosions.AddFile(fx, VFS);
            }));
            tasks.Add(Task.Run(() =>
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
            }));
            tasks.Add(Task.Run(() =>
            {
                //PetalDb
                PetalDb = new PetalDbIni();
                foreach (var pt in Freelancer.PetalDbPaths)
                    PetalDb.AddFile(pt, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                //Hud
                Hud = new HudIni();
                if (string.IsNullOrEmpty(Freelancer.HudPath)) throw new Exception("Need one hud path");
                Hud.AddIni(Freelancer.HudPath, VFS);
                //navbar.ini
                BaseNavBar = new BaseNavBarIni(Freelancer.DataPath, VFS);
                if (!string.IsNullOrEmpty(Freelancer.NavmapPath)) Navmap = new NavmapIni(Freelancer.NavmapPath, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                InfocardMap = new InfocardMapIni();
                InfocardMap.AddMap(Freelancer.DataPath + "/INTERFACE/infocardmap.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
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
            }));
            tasks.Add(Task.Run(() =>
            {
                Formations = new FormationsIni();
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\formations.ini"))
                {
                    Formations.AddFile(Freelancer.DataPath + "MISSIONS\\formations.ini", VFS);
                }
            }));
            tasks.Add(Task.Run(() =>
            {
                News = new NewsIni();
                if (VFS.FileExists(Freelancer.DataPath + "MISSIONS\\news.ini"))
                {
                    News.AddNewsIni(Freelancer.DataPath + "MISSIONS\\news.ini", VFS);
                }
            }));
            tasks.Add(Task.Run(() =>
            {
                Fuses = new FuseIni();
                foreach (var fi in Freelancer.FusePaths)
                    Fuses.AddFuseIni(fi, VFS);
            }));
            //newchardb
            tasks.Add(Task.Run(() =>
            {
                NewCharDB = new NewCharDBIni();
                foreach (var nc in Freelancer.NewCharDBPaths)
                    NewCharDB.AddNewCharDBIni(nc, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Bodyparts = new BodypartsIni(Freelancer.BodypartsPath, this);
                Costumes = new CostumesIni(Freelancer.CostumesPath, this);
            }));
            tasks.Add(Task.Run(() =>
            {
                Audio = new AudioIni();
                foreach (var snd in Freelancer.SoundPaths)
                    Audio.AddIni(snd, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Ships = new ShiparchIni();
                Ships.ParseAllInis(Freelancer.ShiparchPaths, this);
            }));
            tasks.Add(Task.Run(() =>
            {
                Goods = new GoodsIni();
                foreach (var gd in Freelancer.GoodsPaths)
                    Goods.AddGoodsIni(gd, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Markets = new MarketsIni();
                foreach (var mkt in Freelancer.MarketsPaths)
                    Markets.AddMarketsIni(mkt, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                if (VFS.FileExists(Freelancer.DataPath + "missions\\npcships.ini"))
                    NPCShips = new NPCShipIni(Freelancer.DataPath + "missions\\npcships.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Cameras = new CameraIni();
                Cameras.ParseAndFill(Freelancer.CamerasPath, VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                HpTypes = new HpTypesIni();
                HpTypes.LoadDefault();
            }));
            tasks.Add(Task.Run(() =>
            {
                Keymap = new KeymapIni(Freelancer.DataPath + "interface\\keymap.ini", VFS);
                KeyList = new KeyListIni(Freelancer.DataPath + "interface\\keylist.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                InitialWorld = new InitialWorldIni();
                InitialWorld.AddFile(Freelancer.DataPath + "initialworld.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                FactionProps = new FactionPropIni();
                FactionProps.AddFile(Freelancer.DataPath + "missions\\faction_prop.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Empathy = new EmpathyIni();
                Empathy.AddFile(Freelancer.DataPath + "missions\\empathy.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                StateGraphDb = new StateGraphDb(Freelancer.DataPath + "AI\\state_graph.db", VFS);
            }));
            ContentDll = new ContentDll();
            if (VFS.FileExists("DLLS\\BIN\\content.dll"))
                ContentDll.Load(VFS.Resolve("DLLS\\BIN\\content.dll"));
            if (!string.IsNullOrEmpty(Freelancer.DataVersion))
                DataVersion = Freelancer.DataVersion;
            else
                DataVersion = "FL-1";
            Task.WaitAll(tasks.ToArray());
            Loaded = true;
        }
    }
}
