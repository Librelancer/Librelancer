/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;
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

		public static Face LoadSystemFace (Library library, string face)
		{
			return RunningPlatform.LoadSystemFace (library, face);
		}
	}

	public enum OS
	{
		Windows,
		Mac,
		Linux
	}
}

