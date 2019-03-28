// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibreLancer.Data
{
    public static class VFS
    {
        static string FreelancerDirectory;
        static bool CaseSensitive;
        static Dictionary<string, string[]> fileDict;
        public static void Init(string fldir)
        {
            FreelancerDirectory = Path.GetFullPath(fldir).TrimEnd('\\', '/');
            CaseSensitive = Platform.IsDirCaseSensitive(fldir);
            if (CaseSensitive)
            {
                //Provide a fast lookup for files in the directory. Don't follow symlinks
                FLLog.Info("VFS", "Case-Sensitive: Path translation enabled (will impact performance)");
                fileDict = new Dictionary<string, string[]>(StringComparer.CurrentCultureIgnoreCase);
                WalkDir(FreelancerDirectory);
            }
            else
                FLLog.Info("VFS", "Not Case-Sensitive: Path translation disabled");
        }

        static void WalkDir(string dir)
        {
            var f = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
            if (f.Length != 0)
                fileDict.Add(dir.Substring(FreelancerDirectory.Length), f);
            var dinfo = new DirectoryInfo(dir);
            foreach (var directory in dinfo.GetDirectories())
            {
                if(!directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    WalkDir(directory.FullName);
            }
        }

        public static Stream Open(string filename)
        {
            return File.OpenRead(GetPath(filename));
        }
        public static bool FileExists(string filename)
        {
            var fname = GetPath(filename, false);
            if (fname == "VFS:FileMissing") return false;
            return File.Exists(fname);
        }
        public static string GetPath(string filename, bool throwOnError = true)
        {
            if (FreelancerDirectory == null)
                return filename;
            if (File.Exists(filename))
                return filename;
            if (CaseSensitive)
            {
                var p = Path.GetFullPath(Path.Combine(FreelancerDirectory, filename.Replace('\\',Path.DirectorySeparatorChar)));
                if (File.Exists(p)) return p;
                string[] files;
                var dirname = Path.GetDirectoryName(p);
                if (!fileDict.TryGetValue(dirname.Substring(FreelancerDirectory.Length), out files))
                {
                    if (throwOnError)
                        throw new FileNotFoundException(filename);
                    else
                        return "VFS:FileMissing";
                }
                var retval = files.FirstOrDefault(x => x.Equals(p, StringComparison.CurrentCultureIgnoreCase));
                if (retval == null)
                {
                    if (throwOnError) throw new FileNotFoundException(filename);
                    return "VFS:FileMissing";
                }
                return retval;
            }
            return Path.Combine(FreelancerDirectory, filename.Replace('\\', Path.DirectorySeparatorChar));
        }
    }
}