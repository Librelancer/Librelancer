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

namespace LibreLancer
{
	public class GameConfig
	{
		public string FreelancerPath;
		public bool MuteMusic = false;
		public bool IntroMovies = true;
		public string MpvOverride = "";
		public GameConfig ()
		{
		}

		[DllImport("kernel32.dll")]
		static extern bool SetDllDirectory (string directory);

		public void Launch()
		{
			if (Platform.RunningOS == OS.Windows) {
				string bindir = Path.GetDirectoryName (typeof(GameConfig).Assembly.Location);
				var fullpath = Path.Combine (bindir, IntPtr.Size == 8 ? "win64" : "win32");
				SetDllDirectory (fullpath);
			}
			var game = new FreelancerGame (this);
			game.Run ();
		}
	}
}

