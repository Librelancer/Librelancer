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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreLancer.Compatibility
{
    public static class VFS
    {
        static string FreelancerDirectory;
		static bool CaseSensitive;
		public static void Init(string fldir)
		{
			FreelancerDirectory = fldir.TrimEnd ('\\', '/');
			CaseSensitive = Platform.IsDirCaseSensitive (fldir);
			if (CaseSensitive)
				FLLog.Info ("VFS","Case-Sensitive: Path translation enabled (will impact performance)");
			else
				FLLog.Info ("VFS","Not Case-Sensitive: Path translation disabled");
		}
        public static Stream Open(string filename)
        {
            return File.OpenRead(GetPath(filename));
        }
        public static string GetPath(string filename)
        {
			if (FreelancerDirectory == null)
				return filename;
			if (CaseSensitive) {
				var split = filename.Split ('\\', '/');
				var builder = new StringBuilder (FreelancerDirectory.Length + filename.Length);
				builder.Append (FreelancerDirectory);
				builder.Append (Path.DirectorySeparatorChar);
				//Directories
				for (int i = 0; i < split.Length - 1; i++) {
					var curr = builder.ToString ();
					if (Directory.Exists (Path.Combine (curr, split [i]))) {
						builder.Append (split [i]).Append (Path.DirectorySeparatorChar);
					} else {
						bool found = false;
						var s = split [i].ToLowerInvariant ();
						foreach (var dir in Directory.GetDirectories(curr)) {
							var nm = Path.GetFileNameWithoutExtension (dir);
							var lower = Path.GetFileNameWithoutExtension (dir).ToLowerInvariant ();
							if (lower == s) {
								found = true;
								builder.Append (nm).Append (Path.DirectorySeparatorChar);
								break;
							}
						}
						if (!found)
							throw new FileNotFoundException (filename);
					}
				}
				//Find file
				var finaldir = builder.ToString();
				if(File.Exists(Path.Combine(finaldir, split[split.Length - 1])))
					return builder.Append(split[split.Length - 1]).ToString();
				var tofind = split [split.Length - 1].ToLowerInvariant ();
				foreach (var file in Directory.GetFiles(finaldir)) {
					var fn = Path.GetFileName (file).ToLowerInvariant ();
					if (fn == tofind) {
						return builder.Append (Path.GetFileName (file)).ToString ();
					}
				}
				throw new FileNotFoundException (filename);
			}
            return Path.Combine(FreelancerDirectory, filename.Replace('\\', Path.DirectorySeparatorChar));
        }
    }
}
