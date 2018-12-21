// MIT License - Copyright (c) Malte Rupprecht, Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Equipment
{
	public class EquipmentIni : IniFile
	{
		public List<AbstractEquipment> Equip { get; private set; }
        public List<Munition> Munitions { get; private set; }
        public EquipmentIni()
		{
			Equip = new List<AbstractEquipment>();
            Munitions = new List<Munition>();
		}

		public void AddEquipmentIni(string path, FreelancerData data)
		{
			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "light":
					Equip.Add(new Light(s, data));
					break;
				case "power":
                    Equip.Add(FromSection<PowerCore>(s));
					break;
				case "scanner":
					break;
				case "tractor":
					break;
				case "lootcrate":
					break;
				case "repairkit":
					break;
				case "countermeasure":
					break;
				case "countermeasuredropper":
					break;
				case "shieldbattery":
					break;
				case "armor":
					break;
				case "cargopod":
					break;
				case "commodity":
					break;
				case "tradelane":
					break;
				case "internalfx":
                    Equip.Add(FromSection<InternalFx>(s));
					break;
				case "attachedfx":
                    Equip.Add(FromSection<AttachedFx>(s));
					break;
				case "shieldgenerator":
					break;
				case "shield":
					break;
				case "engine":
					break;
				case "thruster":
                    Equip.Add(FromSection<Thruster>(s));
					break;
				case "cloakingdevice":
					break;
				case "motor":
					break;
				case "explosion":
					break;
				case "munition":
                    Munitions.Add(FromSection<Munition>(s));
					break;
				case "gun":
                    Equip.Add(FromSection<Gun>(s));
					break;
				case "mine":
					break;
				case "minedropper":
					break;
				case "lod":
					break;
					default: FLLog.Error("Equipment Ini", "Invalid Section in " + path + ": " + s.Name); break;
				}
			}
		}

		public AbstractEquipment FindEquipment(string nickname)
		{
			IEnumerable<AbstractEquipment> candidates = from AbstractEquipment s in Equip where s.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select s;
			int count = candidates.Count<AbstractEquipment>();
			if (count == 1) return candidates.First<AbstractEquipment>();
			else if (count == 0) return null;
			else throw new Exception(count + " AbstractEquipments with nickname " + nickname);
		}
	}
}
