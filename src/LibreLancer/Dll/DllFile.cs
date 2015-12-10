using System;
using System.IO;
using System.Xml;

namespace LibreLancer.Dll
{
    public class DllFile
    {
		ManagedDllProvider provider;

        public DllFile(string path)
        {
            if (path == null) 
				throw new ArgumentNullException("path");
            using (var file = VFS.Open(path))
            {
                provider = new ManagedDllProvider(file);
            }
        }

		public string GetString(ushort resourceId)
		{
			return provider.GetString (resourceId);
		}

		public XmlDocument GetXml(ushort resourceId)
		{
			return provider.GetXml(resourceId);
		}
    }
}