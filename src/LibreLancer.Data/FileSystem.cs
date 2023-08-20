// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LibreLancer.Data
{
    public class FileSystem
    {
        public List<IFileProvider> FileProviders;

        public FileSystem()
        {
            FileProviders = new List<IFileProvider>();
        }

        public FileSystem(params IFileProvider[] providers)
        {
            FileProviders = new List<IFileProvider>(providers);
        }

        public static FileSystem FromFolder(string folder, bool fastInit = false)
        {
            if (fastInit)
                return new FileSystem(new SysFolderQuickInit(folder));
            else
                return new FileSystem(new SysFolder(folder));
        }

        public Stream Open(string filename)
        {
            foreach (var p in FileProviders)
            {
                Stream stream;
                if ((stream = p.Open(filename)) != null) return stream;
            }

            throw new FileNotFoundException(filename);
        }

        public void Refresh()
        {
            foreach (var f in FileProviders)
                f.Refresh();
        }

        public string Resolve(string filename, bool throwOnError = true)
        {
            foreach (var p in FileProviders)
            {
                string resolved;
                if ((resolved = p.Resolve(filename)) != null) return resolved;
            }

            if (!throwOnError) return null;
            throw new FileNotFoundException(filename);
        }

        public bool FileExists(string filename)
        {
            foreach (var p in FileProviders)
            {
                string resolved;
                if ((resolved = p.Resolve(filename)) != null) return true;
            }

            return false;
        }
    }

    public interface IFileProvider
    {
        Stream Open(string filename);
        string Resolve(string filename);
        void Refresh();
    }

    /// <summary>
    /// Case-insensitive implementation of IFileProvider that does not cache directories on case-sensitive systems
    /// Use when you only want to open a couple of files
    /// </summary>
    public class SysFolderQuickInit : IFileProvider
    {
        private bool caseSensitive;
        private string baseFolder;

        public SysFolderQuickInit(string path)
        {
            caseSensitive = Platform.IsDirCaseSensitive(path);
            baseFolder = path;
        }

        public Stream Open(string filename)
        {
            string fname;
            if ((fname = Resolve(filename)) != null) return File.OpenRead(fname);
            else return null;
        }

        public void Refresh()
        {
            //No-op
        }

        public string Resolve(string filename)
        {
            if (caseSensitive)
            {
                var ogPath = Path.Combine(baseFolder, filename.Replace('\\', Path.DirectorySeparatorChar));
                if (File.Exists(ogPath)) return ogPath;
                var split = filename.Split('\\', '/');
                var builder = new StringBuilder(baseFolder.Length + filename.Length);
                builder.Append(baseFolder);
                builder.Append(Path.DirectorySeparatorChar);
                //Directories
                for (int i = 0; i < split.Length - 1; i++)
                {
                    var curr = builder.ToString();
                    if (Directory.Exists(Path.Combine(curr, split[i])))
                    {
                        builder.Append(split[i]).Append(Path.DirectorySeparatorChar);
                    }
                    else
                    {
                        bool found = false;
                        var s = split[i].ToLowerInvariant();
                        foreach (var dir in Directory.GetDirectories(curr))
                        {
                            var nm = Path.GetFileNameWithoutExtension(dir);
                            if (nm.Equals(s, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                builder.Append(nm).Append(Path.DirectorySeparatorChar);
                                break;
                            }
                        }
                        if (!found) return null;
                    }
                }
                //Find if it is a directory
                var finaldir = builder.ToString();
                if (Directory.Exists(Path.Combine(finaldir, split[split.Length - 1]))) return builder.Append(split[split.Length - 1]).ToString();
                foreach (var dir in Directory.GetDirectories(finaldir))
                {
                    var nm = Path.GetFileNameWithoutExtension(dir);
                    if (nm.Equals(split[split.Length - 1], StringComparison.OrdinalIgnoreCase))
                        return Path.Combine(finaldir, nm);
                }
                //Find file
                if (File.Exists(Path.Combine(finaldir, split[split.Length - 1])))
                    return builder.Append(split[split.Length - 1]).ToString();
                var tofind = split[split.Length - 1].ToLowerInvariant();
                foreach (var file in Directory.GetFiles(finaldir))
                {
                    var fn = Path.GetFileName(file).ToLowerInvariant();
                    if (fn == tofind)
                    {
                        return builder.Append(Path.GetFileName(file)).ToString();
                    }
                }
                //File not found
                return null;
            }
            else
            {
                var path = Path.Combine(baseFolder, filename.Replace('\\', Path.DirectorySeparatorChar));
                if (File.Exists(path)) return path;
                if (Directory.Exists(path)) return path;
                return null;
            }
        }
    }

    /// <summary>
    /// Case-insensitive implementation of IFileProvider
    /// </summary>
    public class SysFolder : IFileProvider
    {
        private bool caseSensitive;
        private string baseFolder;
        
        Dictionary<string, (string, string[])> fileDict;
        
        public SysFolder(string path)
        {
            caseSensitive = Platform.IsDirCaseSensitive(path);
            baseFolder = path;
            Refresh();
        }

        public void Refresh()
        {
            if(caseSensitive) {
                fileDict = new Dictionary<string, (string, string[])>(StringComparer.CurrentCultureIgnoreCase);
                WalkDir(baseFolder);
            }
        }
        
        void WalkDir(string dir, bool recurse = true)
        {
            var f = Directory.EnumerateFiles(dir).Select(x => Path.GetFileName(x)).ToArray();
            if (f.Length != 0)
                fileDict.Add(dir.Substring(baseFolder.Length), (dir,f));
            var dinfo = new DirectoryInfo(dir);
            if (recurse)
            {
                foreach (var directory in dinfo.GetDirectories())
                {
                    if (!directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        WalkDir(directory.FullName);
                    else
                        WalkDir(directory.FullName, false);
                }
            }
        }
        public Stream Open(string filename)
        {
            string fname;
            if ((fname = Resolve(filename)) != null) return File.OpenRead(fname);
            else return null;
        }

        public string Resolve(string filename)
        {
            if (caseSensitive)
            {
                var p = Path.GetFullPath(Path.Combine(baseFolder, filename.Replace('\\',Path.DirectorySeparatorChar)));
                if (File.Exists(p)) return p;
                if (Directory.Exists(p)) return p;
                (string, string[]) files;
                var dirname = Path.GetDirectoryName(p);
                if (p[p.Length - 4] != '.')
                {
                    var fullPath = p.Substring(baseFolder.Length).TrimEnd('\\', Path.DirectorySeparatorChar);
                    foreach (var d in fileDict)
                    {
                        if (d.Key.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return d.Value.Item1;
                        }
                    }
                }
                if (!fileDict.TryGetValue(dirname.Substring(baseFolder.Length), out files)) return null; 
                var fname = Path.GetFileName(p);
                var retval = files.Item2.FirstOrDefault(x => x.Equals(fname, StringComparison.CurrentCultureIgnoreCase));

                if (retval == null)
                {
                    return null;
                }
                return Path.Combine(files.Item1, retval);
            }
            var path = Path.Combine(baseFolder, filename.Replace('\\', Path.DirectorySeparatorChar));
            if (File.Exists(path)) return path;
            if (Directory.Exists(path)) return path;
            return null;
        }
    }
}