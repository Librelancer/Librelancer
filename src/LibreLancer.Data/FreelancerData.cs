// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

using LibreLancer.Data.Equipment;
using LibreLancer.Data.Solar;
using LibreLancer.Data.Characters;
using LibreLancer.Data.Universe;
using LibreLancer.Data.Ships;
using LibreLancer.Data.Audio;
using LibreLancer.Data.Effects;
using LibreLancer.Data.Goods;

namespace LibreLancer.Data
{
	public class FreelancerData
	{
		//Config
		public DacomIni Dacom;
		public FreelancerIni Freelancer;
		//Data
		public InfocardManager Infocards;
		public EffectsIni Effects;
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
		public RichFontsIni RichFonts;
		public PetalDbIni PetalDb;
		public HudIni Hud;
		public BaseNavBarIni BaseNavBar;
		public MBasesIni MBases;
        public ContentDll ContentDll;
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

        public FreelancerData (FreelancerIni fli)
		{
			Freelancer = fli;
		}

		public void LoadData()
		{
			if (Loaded)
				return;
			if(LoadDacom)
				Dacom = new DacomIni ();
			if (Freelancer.JsonResources != null)
			{
				var strs = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(VFS.GetPath("EXE\\" + Freelancer.JsonResources.Item1)));
				var ifcs = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(VFS.GetPath("EXE\\" + Freelancer.JsonResources.Item2)));
				Infocards = new InfocardManager(strs, ifcs);
			}
			else
			{
				Infocards = new InfocardManager(Freelancer.Resources);
			}
            //Build Universe
            var tSolar = new Thread(() =>
            {
                //Equipment
                Equipment = new EquipmentIni();
                foreach (var eq in Freelancer.EquipmentPaths)
                    Equipment.AddEquipmentIni(eq, this);
                Solar = new SolararchIni(Freelancer.SolarPath, this);
                if (Freelancer.StarsPath != null)
                    Stars = new StararchIni(Freelancer.StarsPath);
                else
                    Stars = new StararchIni("DATA\\SOLAR\\stararch.ini");
                Asteroids = new AsteroidArchIni();
                foreach (var ast in Freelancer.AsteroidPaths)
                    Asteroids.AddFile(ast);
                Loadouts = new LoadoutsIni();
                foreach (var lo in Freelancer.LoadoutPaths)
                    Loadouts.AddLoadoutsIni(lo, this);
            });
            var tUniverse = new Thread(() =>
            {
                Universe = new UniverseIni(Freelancer.UniversePath, this);
            });
            //Misc
            var tMisc = new Thread(() =>
            {
                //Graphs
                Graphs = new GraphIni();
                foreach (var g in Freelancer.GraphPaths)
                    Graphs.AddGraphIni(g);
                //Shapes
                EffectShapes = new TexturePanels(Freelancer.EffectShapesPath);
                //Effects
                Effects = new EffectsIni();
                foreach (var fx in Freelancer.EffectPaths)
                    Effects.AddIni(fx);
                //Mouse
                Mouse = new MouseIni(Freelancer.DataPath + "//mouse.ini");
                //Fonts
                RichFonts = new RichFontsIni();
                foreach (var rf in Freelancer.RichFontPaths)
                    RichFonts.AddRichFontsIni(rf);
                //PetalDb
                PetalDb = new PetalDbIni();
                foreach (var pt in Freelancer.PetalDbPaths)
                    PetalDb.AddFile(pt);
                //Hud
                Hud = new HudIni();
                Hud.AddIni(Freelancer.HudPath);
                //navbar.ini
                BaseNavBar = new BaseNavBarIni();
                //mbases.ini
                MBases = new MBasesIni();
            });
            tMisc.Start();
            tSolar.Start();
            tUniverse.Start();
            Bodyparts = new BodypartsIni(Freelancer.BodypartsPath, this);
            Costumes = new CostumesIni(Freelancer.CostumesPath, this);
            Audio = new AudioIni();
            foreach (var snd in Freelancer.SoundPaths)
                Audio.AddIni(snd, Freelancer);
            Ships = new ShiparchIni();
            foreach (var shp in Freelancer.ShiparchPaths)
                Ships.AddShiparchIni(shp, this);
            Goods = new GoodsIni();
            foreach (var gd in Freelancer.GoodsPaths)
                Goods.AddGoodsIni(gd);
            Markets = new MarketsIni();
            foreach (var mkt in Freelancer.MarketsPaths)
                Markets.AddMarketsIni(mkt);
            foreach(var msn in missionFiles)
            {
                if (VFS.FileExists(Freelancer.DataPath + msn))
                    Missions.Add(new Data.Missions.MissionIni(Freelancer.DataPath + msn));
            }
            ContentDll = new ContentDll();
            if (VFS.FileExists("DLLS\\BIN\\content.dll"))
                ContentDll.Load(VFS.GetPath("DLLS\\BIN\\content.dll"));
            tSolar.Join();
            tMisc.Join();
            tUniverse.Join();
            if (!string.IsNullOrEmpty(Freelancer.DataVersion))
                DataVersion = Freelancer.DataVersion;
            else
                DataVersion = "FL-1";
            Loaded = true;
		}
	}
}

