// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

using LibreLancer.Data.Equipment;
using LibreLancer.Data.Solar;
using LibreLancer.Data.Characters;
using LibreLancer.Data.Universe;
using LibreLancer.Data.Ships;
using LibreLancer.Data.Audio;
using LibreLancer.Data.Effects;
using LibreLancer.Data.Goods;
using LibreLancer.Data.Fuses;
using LibreLancer.Data.Interface;
using LibreLancer.Data.NewCharDB;
    
namespace LibreLancer.Data
{
    public class FreelancerData
    {
        //Config
        public DacomIni Dacom;
        public FreelancerIni Freelancer;
        public FileSystem VFS;
        //Data
        public InfocardManager Infocards;
        public EffectsIni Effects;
        public FuseIni Fuses;
        public EquipmentIni Equipment;
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
        public PetalDbIni PetalDb;
        public HudIni Hud;
        public BaseNavBarIni BaseNavBar;
        public MBasesIni MBases;
        public NewCharDBIni NewCharDB;
        public ContentDll ContentDll;
        public InfocardMapIni InfocardMap;
        
        public string DataVersion;
        public bool Loaded = false;

        public bool LoadDacom = true;

        public List<Missions.MissionIni> Missions = new List<Missions.MissionIni>();
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

        public FreelancerData (FreelancerIni fli, FileSystem vfs)
        {
            Freelancer = fli;
            VFS = vfs;
        }

        public void LoadData()
        {
            if (Loaded)
                return;
            if(LoadDacom)
                Dacom = new DacomIni (VFS);
            if (Freelancer.JsonResources != null)
            {
                Infocards = new InfocardManager(File.ReadAllText(Freelancer.JsonResources.Item1), File.ReadAllText(Freelancer.JsonResources.Item2));
            }
            else
            {
                Infocards = new InfocardManager(Freelancer.Resources);
            }

            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() =>
            {
                Equipment = new EquipmentIni();
                foreach (var eq in Freelancer.EquipmentPaths)
                    Equipment.AddEquipmentIni(eq, this);
            }));
            tasks.Add(Task.Run(() =>
            {
                Solar = new SolararchIni(Freelancer.SolarPath, this);
                if (Freelancer.StarsPath != null)
                    Stars = new StararchIni(Freelancer.StarsPath, VFS);
                else
                    Stars = new StararchIni("DATA\\SOLAR\\stararch.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                Asteroids = new AsteroidArchIni();
                foreach (var ast in Freelancer.AsteroidPaths)
                    Asteroids.AddFile(ast, VFS);
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
                //Mouse
                Mouse = new MouseIni(Freelancer.DataPath + "/mouse.ini", VFS);
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
                Hud.AddIni(Freelancer.HudPath, VFS);
                //navbar.ini
                BaseNavBar = new BaseNavBarIni(VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                InfocardMap = new InfocardMapIni();
                InfocardMap.AddMap(Freelancer.DataPath + "/INTERFACE/infocardmap.ini", VFS);
            }));
            tasks.Add(Task.Run(() =>
            {
                //mbases.ini
                MBases = new MBasesIni(VFS);
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
                foreach (var shp in Freelancer.ShiparchPaths)
                    Ships.AddShiparchIni(shp, this);
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
                foreach (var msn in missionFiles)
                {
                    if (VFS.FileExists(Freelancer.DataPath + msn))
                        Missions.Add(new Data.Missions.MissionIni(Freelancer.DataPath + msn, VFS));
                }
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