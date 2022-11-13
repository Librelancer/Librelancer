// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using LibreLancer.Dll;

namespace LibreLancer.Data
{
	public class InfocardManager
	{
		Dictionary<int,string> strings = new Dictionary<int, string>();
		Dictionary<int, string> infocards = new Dictionary<int, string>();
		public InfocardManager (List<DllFile> res)
		{
			int i = 0;
			foreach (var file in res) {
				foreach (var k in file.Strings.Keys)
                {
                    var str = file.Strings[k];
                    //HACK: Figure out what to do with %M strings
                    if (str.EndsWith("%M")) str = str.Substring(0, str.Length - 2);
					strings.Add (k + (i * 65536), str);
				}
				foreach (var k in file.Infocards.Keys) {
					infocards.Add (k + (i * 65536), file.Infocards [k]);
				}
				i++;
			}
		}

        class JsonContainer
        {
            public string filetype;
            public Dictionary<int, string> data;
        }

		public InfocardManager(List<string> jsonFiles, FileSystem vfs)
        {
            strings = new Dictionary<int, string>();
            infocards = new Dictionary<int, string>();
            foreach (var f in jsonFiles)
            {
                using (var reader = new StreamReader(vfs.Open(f)))
                {
                    var file = JSON.Deserialize<JsonContainer>(reader.ReadToEnd());
                    if(string.IsNullOrEmpty(file.filetype)) throw new Exception($"{f} is not a valid resource file");
                    if(file.data == null) continue;
                    if (file.filetype.Equals("strings", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var kv in file.data)
                        {
                            try
                            {
                                strings.Add(kv.Key, kv.Value);
                            }
                            catch (ArgumentException)
                            {
                                throw new Exception($"{f} trying to add existing IDS {kv.Key}");
                            }
                        }
                    } 
                    else if (file.filetype.Equals("infocards", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var kv in file.data)
                        {
                            try
                            {
                                infocards.Add(kv.Key, kv.Value);
                            }
                            catch (ArgumentException)
                            {
                                throw new Exception($"{f} trying to add existing IDS {kv.Key}");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid filetype in {f} (expected strings or infocards)");
                    }
                }
            }
        }

      

        public void ExportStrings(string filename)
		{
			using (var writer = new StreamWriter(filename))
            {
                var obj = new JsonContainer() {filetype = "strings", data = strings};
                writer.Write(JSON.Serialize(obj));
            }
		}

		public void ExportInfocards(string filename)
		{
			using (var writer = new StreamWriter(filename))
			{
                var obj = new JsonContainer() {filetype = "infocards", data = infocards};
                writer.Write(JSON.Serialize(obj));
            }
		}

        public IEnumerable<int> StringIds => strings.Keys.OrderBy(x => x);
        public IEnumerable<int> InfocardIds => infocards.Keys.OrderBy(x => x);

        public IEnumerable<KeyValuePair<int, string>> AllStrings => strings;
        public IEnumerable<KeyValuePair<int, string>> AllXml => infocards;

        List<int> missingStrings = new List<int>();
		public string GetStringResource(int id)
		{
            if (id == 0) return "";
			if (strings.ContainsKey (id)) {
                return strings [id];
			} else {
                if (!missingStrings.Contains(id))
                {
                    FLLog.Warning("Strings", "Not Found: " + id);
                    missingStrings.Add(id);
                }
				return "";
			}
		}

        List<int> missingXml = new List<int>();
		public string GetXmlResource(int id)
		{
            if (id == 0) return null;
			if (infocards.ContainsKey (id)) {
				return infocards [id];
			} else {
                if (!missingXml.Contains(id))
                {
                    FLLog.Warning("Infocards", "Not Found: " + id);
                    missingXml.Add(id);
                }
				return null;
			}
		}
	}
}

