// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Xml;
using LibreLancer.Data;
namespace LibreLancer.Dll
{
    public class DllFile
    {
		public string Name;
		ManagedDllProvider provider;

        public DllFile(string path, FileSystem vfs)
        {
			
            if (path == null) 
				throw new ArgumentNullException("path");
			Name = Path.GetFileName (path);
            if (vfs == null)
            {
                using (var file = File.OpenRead(path))
                {
                    provider = new ManagedDllProvider(file, Name);
                }
            }
            else
            {
                using (var file = vfs.Open(path))
                {
                    provider = new ManagedDllProvider(file, Name);
                }
            }
        }

        public VersionInfoResource VersionInfo => provider.VersionInfo;

        public Dictionary<int, string> Strings => provider.Strings;
        public Dictionary<int, string> Infocards => provider.Infocards;

        public List<BinaryResource> Dialogs => provider.Dialogs;
        
        public List<BinaryResource> Menus => provider.Menus;

    }
}