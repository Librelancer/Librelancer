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
        public static void Register(Assembly assembly)
        {
            NativeLibrary.SetDllImportResolver(assembly, MapAndLoad);
        }
        private static IntPtr MapAndLoad(string libraryName, Assembly assembly,
            DllImportSearchPath? dllImportSearchPath)
        {
            string mappedName = null;
            mappedName = MapLibraryName(assembly.Location, libraryName, out mappedName) ? mappedName : libraryName;
            return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
        }

        static Dictionary<string, string> mapped = new Dictionary<string, string>();
        private static bool MapLibraryName(string assemblyLocation, string originalLibName, out string mappedLibName)
        {
            mappedLibName = null;
            if (mapped.TryGetValue(originalLibName, out mappedLibName))
                return true;
            string xmlPath = assemblyLocation + ".config";
            if (!File.Exists(xmlPath))
                return TryFind(assemblyLocation, originalLibName, out mappedLibName);
            FLLog.Debug("DllMap", xmlPath);
            XElement root = XElement.Load(xmlPath);
            var map =
                (from el in root.Elements("dllmap")
                    where ((string) el.Attribute("dll") == originalLibName) && ((string) el.Attribute("os") == "linux")
                    select el).SingleOrDefault();
            if (map != null)
            {
                mappedLibName = map.Attribute("target").Value;
                mapped.Add(originalLibName, mappedLibName);
            }

            return (mappedLibName != null);
        }

        static bool TryFind(string assemblyLocation, string originalLibName, out string mappedLibName)
        {
            mappedLibName = null;
            return false;
        }
    }
}