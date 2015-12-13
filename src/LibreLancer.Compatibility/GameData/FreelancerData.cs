using System;

using LibreLancer.Compatibility.GameData.Equipment;
using LibreLancer.Compatibility.GameData.Solar;
using LibreLancer.Compatibility.GameData.Characters;
using LibreLancer.Compatibility.GameData.Universe;
namespace LibreLancer.Compatibility.GameData
{
	public class FreelancerData
	{
		//Config
		public DacomIni Dacom;
		public FreelancerIni Freelancer;
		//Data
		public InfocardManager Infocards;
		public EquipmentIni Equipment;
		public LoadoutsIni Loadouts;
		public SolararchIni Solar;
		public StararchIni Stars;
		public BodypartsIni Bodyparts;
		public CostumesIni Costumes;
		public UniverseIni Universe;
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
			Stars = new StararchIni (Freelancer.StarsPath);
			//Loadouts
			Loadouts = new LoadoutsIni();
			foreach (var lo in Freelancer.LoadoutPaths)
				Loadouts.AddLoadoutsIni (lo, this);
			//Universe
			Universe = new UniverseIni(Freelancer.UniversePath, this);
			Loaded = true;
		}
	}
}

