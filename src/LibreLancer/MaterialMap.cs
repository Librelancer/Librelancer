using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace LibreLancer
{
	public class MaterialMap
	{
		public static MaterialMap Instance {
			get {
				return _instance;
			}
		}
		static MaterialMap _instance;
		Dictionary<string,string> maps = new Dictionary<string, string> ();
		List<MapEntry> regexmaps = new List<MapEntry>();
		public MaterialMap()
		{
			if (_instance != null)
				throw new Exception ("Only one MaterialMap can be made");
			_instance = this;
		}
		class MapEntry
		{
			public Regex Regex;
			public string Value;
			public MapEntry(Regex r, string v)
			{
				Regex = r;
				Value = v;
			}
		}
		public string Get(string val)
		{
			//Evaluate bottom to top
			for (int i = regexmaps.Count - 1; i >= 0; i--) {
				if (regexmaps [i].Regex.IsMatch (val)) {
					FLLog.Debug ("MaterialMap", "Matched " + val + " to " + regexmaps [i].Value);
					return regexmaps [i].Value;
				}
			}

			if (maps.ContainsKey (val)) {
				FLLog.Debug ("MaterialMap", "Matched " + val + " to " + maps [val]);
				return maps [val];
			}
			
			return val;
		}
		public void AddRegex(StringKeyValue kv)
		{
			regexmaps.Add (new MapEntry(new Regex (kv.Key), kv.Value));
		}
		public void AddMap(string k, string v)
		{
			maps.Add (k, v);
		}
	}
}

