using System;
using LibreLancer.Dll;
using System.Xml;
using System.Collections.Generic;
namespace LibreLancer.Compatibility.GameData
{
	public class InfocardManager
	{
		List<DllFile> resources;
		public InfocardManager (List<DllFile> res)
		{
			resources = res;
		}
		public string GetStringResource(int id)
		{
			int fileId = id % 65536;
			if (fileId >= 0 && fileId < resources.Count)
			{
				ushort resId = (ushort)id;
				return resources[fileId].GetString(resId);
			}
			else return string.Empty;
		}

		public XmlDocument GetXmlResource(int id)
		{
			int fileId = id % 65536;
			if (fileId >= 0 && fileId < resources.Count)
			{
				ushort resId = (ushort)id;
				XmlDocument result = resources[fileId].GetXml(resId);
				/*if (result == null)
                {
                    result = new XmlDocument();
                    result.Load(Resources[fileId].GetString(resId));
                }*/
				return result;
			}
			else return null;
		}
	}
}

