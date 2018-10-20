// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Reflection;
using LibreLancer.Platforms;
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
	}

	public enum OS
	{
		Windows,
		Mac,
		Linux
	}
}

