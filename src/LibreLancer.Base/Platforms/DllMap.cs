// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace LibreLancer
{
    //Emulate mono's DllImport behaviour on Linux
    static class DllMap
    {
        private static Dictionary<string, string> libs = new Dictionary<string, string>();

        public static void Register(Assembly assembly)
        {
            lock (libs)
            {
                string xmlPath = assembly.Location + ".config";
                if (File.Exists(xmlPath))
                {
                    foreach (var el in XElement.Load(xmlPath).Elements("dllmap"))
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            if (!el.Attribute("os").ToString().Contains("osx", StringComparison.OrdinalIgnoreCase))
                                continue;
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            if (!el.Attribute("os").ToString().Contains("linux", StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        string oldLib = el.Attribute("dll").Value;
                        string newLib = el.Attribute("target").Value;
                        if (string.IsNullOrWhiteSpace(oldLib) || string.IsNullOrWhiteSpace(newLib))
                            continue;
                        libs[oldLib] = newLib;
                    }
                }
            }
            NativeLibrary.SetDllImportResolver(assembly, MapAndLoad);
        }
        private static IntPtr MapAndLoad(string libraryName, Assembly assembly,
            DllImportSearchPath? dllImportSearchPath)
        {
            string mappedName = null;
            lock (libs) {
                mappedName = libs.TryGetValue(libraryName, out mappedName) ? mappedName : libraryName;
            }
            return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
        }

    }
}
