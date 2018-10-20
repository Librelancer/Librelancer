// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
		public static string GetPath(string filename, bool throwOnError = true)
        {
			if (FreelancerDirectory == null)
				return filename;
			if (File.Exists(filename))
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
							if (throwOnError)
								throw new FileNotFoundException(filename);
							else
								return "VFS:FileMissing";

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
				return builder.Append(split[split.Length - 1]).ToString(); //Hack around mods with missing files
				//throw new FileNotFoundException (filename);
			}
            return Path.Combine(FreelancerDirectory, filename.Replace('\\', Path.DirectorySeparatorChar));
        }
    }
}
