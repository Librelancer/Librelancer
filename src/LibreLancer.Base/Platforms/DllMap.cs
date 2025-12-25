// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace LibreLancer.Platforms;

//Emulate mono's DllImport behaviour on Linux
internal static class DllMap
{
    private static Dictionary<string, string> libs = new Dictionary<string, string>();

    public static void Register(Assembly assembly)
    {
        lock (libs)
        {
            var xmlPath = assembly.Location + ".config";

            if (File.Exists(xmlPath))
            {
                foreach (var el in XElement.Load(xmlPath).Elements("dllmap"))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        if (!el.Attribute("os")!.ToString().Contains("osx", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        if (!el.Attribute("os")!.ToString().Contains("linux", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    var oldLib = el.Attribute("dll")?.Value;
                    var newLib = el.Attribute("target")?.Value;

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
        string? mappedName = null;

        lock (libs)
        {
            mappedName = libs.TryGetValue(libraryName, out mappedName) ? mappedName : libraryName;
        }

        return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
    }
}
