using System;

using LibreLancer.Compatibility.GameData.Equipment;
using LibreLancer.Compatibility.GameData.Solar;
using LibreLancer.Compatibility.GameData.Characters;
using LibreLancer.Compatibility.GameData.Universe;
using LibreLancer.Compatibility.GameData.Ships;
using LibreLancer.Compatibility.GameData.Audio;
using LibreLancer.Compatibility.GameData.Effects;

namespace LibreLancer.Compatibility.GameData
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
		public GraphIni Graphs;
		public TexturePanels EffectShapes;
		public MouseIni Mouse;
		public AsteroidArchIni Asteroids;
		public RichFontsIni RichFonts;
		public PetalDbIni PetalDb;
		public HudIni Hud;
		public bool Loaded = false;

		public FreelancerData (FreelancerIni fli)
		{
			Freelancer = fli;
		}

		public void LoadData()
		{
			if (Loaded)
				return;
			Dacom = new DacomIni ();
			Infocards = new InfocardManager (Freelancer.Resources);
			//Dfm
			Bodyparts = new BodypartsIni (Freelancer.BodypartsPath, this);
			Costumes = new CostumesIni (Freelancer.CostumesPath, this);
			//Equipment
			Equipment = new EquipmentIni();
			foreach (var eq in Freelancer.EquipmentPaths)
				Equipment.AddEquipmentIni (eq, this);
			//Solars
			Solar = new SolararchIni (Freelancer.SolarPath, this);
			if(Freelancer.StarsPath != null)
				Stars = new StararchIni (Freelancer.StarsPath);
			Asteroids = new AsteroidArchIni ();
			foreach (var ast in Freelancer.AsteroidPaths)
				Asteroids.AddFile (ast);
			//Loadouts
			Loadouts = new LoadoutsIni();
			foreach (var lo in Freelancer.LoadoutPaths)
				Loadouts.AddLoadoutsIni (lo, this);
			//Universe
			Universe = new UniverseIni(Freelancer.UniversePath, this);
			//Ships
			Ships = new ShiparchIni();
			foreach (var shp in Freelancer.ShiparchPaths)
				Ships.AddShiparchIni (shp, this);
			//Audio
			Audio = new AudioIni();
			foreach (var snd in Freelancer.SoundPaths)
				Audio.AddIni(snd, Freelancer);
			Loaded = true;
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
		}
	}
}

