// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Data.Ships
{
	public class ShiparchIni : IniFile
	{
		public List<Ship> Ships = new List<Ship>();

		public ShiparchIni ()
		{
		}
		public void AddShiparchIni(string path, FreelancerData fldata)
		{
			foreach (Section s in ParseFile(path)) {
                if (s.Name.ToLowerInvariant() == "ship")
                    Ships.Add(FromSection<Ship>(s));
			}
		}

		public Ship GetShip(string name)
		{
			
			IEnumerable<Ship> result = from Ship s in Ships where s.Nickname == name select s;
			if (result.Count<Ship> () == 1)
				return result.First<Ship> ();
			else
				throw new Exception ();
		}
	}
}

