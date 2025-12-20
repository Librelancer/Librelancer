// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Text.RegularExpressions;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema
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
				FLLog.Warning("MaterialMap", "Switching global material map. TODO make instance");
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
					return regexmaps [i].Value;
				}
			}

			if (maps.ContainsKey (val)) {
				return maps [val];
			}

			return null;
		}
		public void AddRegex(StringKeyValue kv)
		{
			regexmaps.Add (new MapEntry(new Regex (kv.Key), kv.Value));
		}

        public void AddRegex(string k, string v)
        {
            AddRegex(new StringKeyValue(k, v));
        }
		public void AddMap(string k, string v)
		{
			maps.Add (k, v);
		}
	}
}

