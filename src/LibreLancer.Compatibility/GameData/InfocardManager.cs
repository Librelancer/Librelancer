using System;
using LibreLancer.Dll;
using System.Xml;
using System.Collections.Generic;
namespace LibreLancer.Compatibility.GameData
{
	public class InfocardManager
	{
		Dictionary<int,string> strings = new Dictionary<int, string>();
		Dictionary<int, XmlDocument> infocards = new Dictionary<int, XmlDocument>();
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

		public XmlDocument GetXmlResource(int id)
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

