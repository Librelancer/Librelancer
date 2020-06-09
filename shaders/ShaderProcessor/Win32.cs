// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace ShaderProcessor
{
    public static class Win32
    {
        //Try really hard to find an exe
        //Should be somewhere
        public static string ResolveExecPath(string exe)
        {
            if (File.Exists(exe)) return Path.GetFullPath(exe);
            return GetFullPathFromWindows(exe, new[]
            {
                Environment.CurrentDirectory,
                GetBasePath(),
                AppDomain.CurrentDomain.BaseDirectory
            });
        }
        static string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }
        static string GetFullPathFromWindows(string exeName, string[] extraPaths)
        {
            StringBuilder sb = new StringBuilder(exeName, MAX_PATH);
            return PathFindOnPath(sb, extraPaths) ? sb.ToString() : null;
        }
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);
        private const int MAX_PATH = 260;
    }
}