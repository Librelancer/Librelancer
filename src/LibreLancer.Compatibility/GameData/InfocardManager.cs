/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Dll;
using Newtonsoft.Json;

namespace LibreLancer.Compatibility.GameData
{
	public class InfocardManager
	{
		Dictionary<int,string> strings = new Dictionary<int, string>();
		Dictionary<int, string> infocards = new Dictionary<int, string>();
		public InfocardManager (List<DllFile> res)
		{
			int i = 0;
			foreach (var file in res) {
				foreach (var k in file.Strings.Keys) {
					strings.Add (k + (i * 65536), file.Strings [k]);
				}
				foreach (var k in file.Infocards.Keys) {
					infocards.Add (k + (i * 65536), file.Infocards [k]);
				}
				i++;
			}
		}
		public InfocardManager(Dictionary<int, string> strings, Dictionary<int, string> infocards)
		{
			this.strings = strings;
			this.infocards = infocards;
		}

		public void ExportStrings(string filename)
		{
			using (var writer = new StreamWriter(filename))
			{
				writer.Write(JsonConvert.SerializeObject(strings, Formatting.Indented));
			}
		}

		public void ExportInfocards(string filename)
		{
			using (var writer = new StreamWriter(filename))
			{
				writer.Write(JsonConvert.SerializeObject(infocards, Formatting.Indented));
			}
		}

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

