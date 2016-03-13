using System;
using System.IO;
using System.Runtime.InteropServices;
using LibreLancer.Platforms;

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
	}

	public enum OS
	{
		Windows,
		Mac,
		Linux
	}
}

