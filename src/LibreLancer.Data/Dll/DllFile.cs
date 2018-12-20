// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using LibreLancer.Data;
namespace LibreLancer.Dll
{
    public class DllFile
    {
		public string Name;
		ManagedDllProvider provider;

        public DllFile(string path)
        {
			
            if (path == null) 
				throw new ArgumentNullException("path");
			Name = Path.GetFileName (path);
            using (var file = VFS.Open(path))
            {
                provider = new ManagedDllProvider(file);
            }
        }

		public Dictionary<int,string> Strings {
			get {
				return provider.Strings;
			}
		}

		public Dictionary<int, string> Infocards {
			get {
				return provider.Infocards;
			}
		}
    }
}