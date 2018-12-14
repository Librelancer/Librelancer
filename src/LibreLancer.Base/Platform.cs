// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Reflection;
using LibreLancer.Platforms;
using LibreLancer.Dialogs;
using SharpFont;

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
					RunningPlatform = new MacPlatform ();
				} else {
					RunningOS = OS.Linux;
					RunningPlatform = new LinuxPlatform ();
				}
				break;
			case PlatformID.MacOSX:
				RunningOS = OS.Mac;
				RunningPlatform = new MacPlatform ();
				break;
			default:
				RunningOS = OS.Windows;
				RunningPlatform = new Win32Platform ();
				break;
			}
		}


		public static bool IsDirCaseSensitive (string directory)
		{
			return RunningPlatform.IsDirCaseSensitive (directory);
		}

		public static Face LoadSystemFace (Library library, string face, ref FontStyles style)
		{
			return RunningPlatform.LoadSystemFace(library, face, ref style);
		}

		public static Face GetFallbackFace(Library library, uint cp)
		{
			return RunningPlatform.GetFallbackFace(library, cp);
		}

        public static string GetInformationalVersion<T>()
        {
            return ((AssemblyInformationalVersionAttribute)Assembly
                .GetAssembly(typeof(T))
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0])
                .InformationalVersion;
        }
        //Make it hard to crash with a cryptic message at startup
        const string V2012_64 = "Librelancer requires Visual C++ 2012 redistributable. Download from: https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe";
        const string V2012_32 = "Librelancer requires Visual C++ 2012 redistributable. Download from: https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe";
        const string V2015_64 = "Librelancer requires Visual C++ 2015 redistributable. Download from: https://download.microsoft.com/download/6/A/A/6AA4EDFF-645B-48C5-81CC-ED5963AEAD48/vc_redist.x64.exe";
        const string V2015_32 = "Librelancer requires Visual C++ 2015 redistributable. Download from: https://download.microsoft.com/download/6/A/A/6AA4EDFF-645B-48C5-81CC-ED5963AEAD48/vc_redist.x86.exe";

        static bool CheckVCKey(string v,string key)
        {
            var ver = Microsoft.Win32.Registry.GetValue(key, "Version", null);
            if (ver == null || !ver.ToString().StartsWith(key, StringComparison.Ordinal)) return false;
            return true;
        }

        public static bool CheckDependencies()
        {
            if (RunningOS != OS.Windows) return true;
            if (IntPtr.Size == 8)
            {
                if (!CheckVCKey("11", @"HKEY_LOCAL_MACHINE\Software\Classes\Installer\Dependencies\{ca67548a-5ebe-413a-b50c-4b9ceb6d66c6}"))
                {
                    CrashWindow.Run("Librelancer", "Missing Components", V2012_64);
                    return false;
                }
                if (!CheckVCKey("14", @"HKEY_LOCAL_MACHINE\Software\Classes\Installer\Dependencies\{d992c12e-cab2-426f-bde3-fb8c53950b0d}"))
                {
                    CrashWindow.Run("Librelancer", "Missing Components", V2015_64);
                    return false;
                }
            }
            else
            {
                if (!CheckVCKey("11", @"HKEY_LOCAL_MACHINE\Software\Classes\Installer\Dependencies\{33d1fd90-4274-48a1-9bc1-97e33d9c2d6f}"))
                {
                    CrashWindow.Run("Librelancer", "Missing Components", V2012_32);
                    return false;
                }
                if (!CheckVCKey("14", @"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Dependencies\{e2803110-78b3-4664-a479-3611a381656a}"))
                {
                    CrashWindow.Run("Librelancer", "Missing Components", V2015_32);
                    return false;
                }
            }
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

