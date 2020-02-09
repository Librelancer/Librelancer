// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LibreLancer.Platforms;
using LibreLancer.Dialogs;

namespace LibreLancer
{
	public static class Platform
	{
		public static OS RunningOS;
		static IPlatform RunningPlatform;

		static Platform ()
		{
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Unix:
				if (Directory.Exists ("/Applications")
				    & Directory.Exists ("/System")
				    & Directory.Exists ("/Users")
				    & Directory.Exists ("/Volumes")) {
					RunningOS = OS.Mac;
                    throw new NotImplementedException("macOS");
                } else {
					RunningOS = OS.Linux;
					RunningPlatform = new LinuxPlatform ();
				}
				break;
			case PlatformID.MacOSX:
				RunningOS = OS.Mac;
                throw new NotImplementedException("macOS");
			default:
				RunningOS = OS.Windows;
				RunningPlatform = new Win32Platform ();
				break;
			}
            RegisterDllMap(typeof(Platform).Assembly);
		}
        public static bool IsDirCaseSensitive (string directory)
		{
			return RunningPlatform.IsDirCaseSensitive (directory);
		}

        public static void RegisterDllMap(Assembly assembly)
        {
            if(RunningOS == OS.Linux)
                DllMap.Register(assembly);
        }
        public static byte[] GetMonospaceBytes()
        {
            return RunningPlatform.GetMonospaceBytes();
        }

        internal static List<string> LoadedTTFs = new List<string>();
        internal static event Action FontLoaded;
        public static void AddTtfFile(string file)
        {
            if (LoadedTTFs.Contains(file)) return;
            if (!File.Exists(file)) throw new FileNotFoundException(file);
            LoadedTTFs.Add(file);
            RunningPlatform.AddTtfFile(file);
            FontLoaded?.Invoke();
        }

        public static string GetInformationalVersion<T>()
        {
            return ((AssemblyInformationalVersionAttribute)Assembly
                .GetAssembly(typeof(T))
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0])
                .InformationalVersion;
        }
        //Make it hard to crash with a cryptic message at startup
        const string V2012_64 = "Librelancer requires Visual C++ 2012 redistributable (x64). Download from: https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe";
        const string V2012_32 = "Librelancer requires Visual C++ 2012 redistributable (x86). Download from: https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe";
        const string V2015_64 = "Librelancer requires the Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019 (x64). Download from: https://aka.ms/vs/16/release/vc_redist.x64.exe";
        const string V2015_32 = "Librelancer requires the Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019 (x86). Download from: https://aka.ms/vs/16/release/vc_redist.x86.exe";

        static bool CheckVCRun(string file, string errx64, string errx86)
        {
            if (LoadLibrary(file) == IntPtr.Zero)
            {
                CrashWindow.Run("Librelancer", "Missing Components", IntPtr.Size == 8 ? errx64 : errx86);
                return false;
            }
            return true;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpLibFileName);

        public static bool CheckDependencies()
        {
            if (RunningOS != OS.Windows) return true;
            if (!CheckVCRun("msvcr110.dll", V2012_64, V2012_32)) return false;
            if (!CheckVCRun("vcruntime140.dll", V2015_64, V2015_32)) return false;
            if (!CheckVCRun("vcruntime140_1.dll", V2015_64, V2015_32)) return false;
            return true;
        }
    }

	public enum OS
	{
		Windows,
		Mac,
		Linux
	}
}

