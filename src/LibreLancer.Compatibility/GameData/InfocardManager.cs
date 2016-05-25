using System;
using LibreLancer.Dll;
using System.Xml;
using System.Collections.Generic;
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
		public string GetStringResource(int id)
		{
			if (strings.ContainsKey (id)) {
				return strings [id];
			} else {
				FLLog.Warning ("Infocards","Not Found: " + id);
				return "";
			}
		}

		public string GetXmlResource(int id)
		{
			if (infocards.ContainsKey (id)) {
				return infocards [id];
			} else {
				FLLog.Warning ("Infocards","Not Found: " + id);
				return null;
			}
		}
	}
}

