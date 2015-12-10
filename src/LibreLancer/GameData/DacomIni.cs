using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.GameData
{
	public class DacomIni : IniFile
	{
		public MaterialMap MaterialMap { get; private set; }
		public DacomIni ()
		{
			foreach (Section s in ParseFile("EXE\\dacom.ini", true)) {
				switch (s.Name.ToLowerInvariant ()) {
				case "materialmap":
					var map = new MaterialMap ();
					foreach (Entry e in s) {
						if (e.Name.ToLowerInvariant () != "Name") {
							map.AddMap (e.Name, e [0].ToString ());
						} else {
							map.AddRegex (e [0].ToKeyValue ());
						}
					}
					break;
				default:
					break;
				}

			}
		}
	}
}

