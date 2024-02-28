// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibreLancer.Data.IO
{
    public class FileSystem
    {
        public List<IFileProvider> FileProviders;

        public FileSystem()
        {
            FileProviders = new List<IFileProvider>();
        }

        private static readonly char[] seps = new[] { '/', '\\' };
        public string RemovePathComponent(string path)
        {
            var idx = path.LastIndexOfAny(seps);
            string iniFolder;
            if (idx == -1)
                return "";
            else
                return path.Substring(0, idx + 1);
        }

        public FileSystem(params IFileProvider[] providers)
        {
            FileProviders = new List<IFileProvider>(providers);
        }

        public static FileSystem FromPath(string folder, bool fastInit = false)
        {
            if (Directory.Exists(folder))
            {
                if (fastInit)
                    return new FileSystem(new SysFolderQuickInit(folder));
                else
                    return new FileSystem(new SysFolder(folder));
            }
            else if (File.Exists(folder))
            {
                using var stream = File.OpenRead(folder);
                if (ZipFileSystem.IsZip(stream))
                    return new FileSystem(new ZipFileSystem(folder));
                if (LrpkFileSystem.IsLrpk(stream))
                    return new FileSystem(new LrpkFileSystem(folder));
            }
            throw new DirectoryNotFoundException(folder);
        }

        public Stream Open(string filename)
        {
            for (int i = FileProviders.Count - 1; i >= 0; i--)
            {
                Stream stream;
                if ((stream = FileProviders[i].Open(filename)) != null) return stream;
            }
            throw new FileNotFoundException(filename);
        }

        public void Refresh()
        {
            foreach (var f in FileProviders)
                f.Refresh();
        }

        public string ReadAllText(string filename)
        {
            using var stream = Open(filename);
            return new StreamReader(stream).ReadToEnd();
        }

        public byte[] ReadAllBytes(string filename)
        {
            using var stream = Open(filename);
            var mem = new MemoryStream();
            stream.CopyTo(mem);
            return mem.ToArray();
        }

        public string[] GetFiles(string path)
        {
            HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = FileProviders.Count - 1; i >= 0; i--)
            {
                foreach (var f in FileProviders[i].GetFiles(path))
                    files.Add(f);
            }
            return files.ToArray();
        }

        public string[] GetDirectories(string path)
        {
            HashSet<string> dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = FileProviders.Count - 1; i >= 0; i--)
            {
                foreach (var f in FileProviders[i].GetDirectories(path))
                    dirs.Add(f);
            }
            return dirs.ToArray();
        }

        public string GetBackingFileName(string path)
        {
            for (int i = FileProviders.Count - 1; i >= 0; i--)
            {
                if (FileProviders[i].GetBackingFileName(path, out var fname))
                    return fname;
            }
            return null;
        }

        public bool FileExists(string filename)
        {
            for (int i = FileProviders.Count - 1; i >= 0; i--)
            {
                if (FileProviders[i].FileExists(filename))
                    return true;
            }
            return false;
        }
    }

    public interface IFileProvider
    {
        Stream Open(string filename);
        bool FileExists(string filename);
        bool GetBackingFileName(string path, out string filename);
        IEnumerable<string> GetFiles(string path);
        IEnumerable<string> GetDirectories(string path);
        void Refresh();
    }

    /// <summary>
    /// Case-insensitive implementation of IFileProvider that does not cache directories on case-sensitive systems
    /// Use when you only want to open a couple of files
    /// </summary>
    public sealed class SysFolderQuickInit : IFileProvider
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

        public IEnumerable<string> GetDirectories(string path)
        {
            var directory = Resolve(path);
            if (directory == null)
                return Array.Empty<string>();
            return Directory.GetDirectories(directory).Select(Path.GetFileName).ToArray();
        }

        public void Refresh()
        {
            //No-op
        }

        public bool FileExists(string filename) => Resolve(filename) != null;

        public bool GetBackingFileName(string path, out string filename)
        {
            filename = Resolve(path);
            return filename != null;
        }

        public IEnumerable<string> GetFiles(string path)
        {
            var directory = Resolve(path);
            if (directory == null)
                return Array.Empty<string>();
            return Directory.GetFiles(directory).Select(Path.GetFileName).ToArray();
        }

        string Resolve(string filename)
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
    public sealed class SysFolder : BaseFileSystemProvider
    {
        private bool caseSensitive;
        private string baseFolder;

        public SysFolder(string path)
        {
            caseSensitive = Platform.IsDirCaseSensitive(path);
            baseFolder = path;
            Refresh();
        }

        public override void Refresh()
        {
            if(caseSensitive) {
                Root = WalkDir(baseFolder, null);
                Root.Name = "";
            }
        }

        class SysFile : VfsFile
        {
            public string FullPath;
            public SysFile(string path, string name)
            {
                Name = name;
                FullPath = path;
            }
            public override Stream OpenRead() => File.OpenRead(FullPath);
            public override string GetBackingFilename() => FullPath;
        }

        public override IEnumerable<string> GetFiles(string path)
        {
            var fullPath = Path.Combine(baseFolder, path.Replace('\\', Path.DirectorySeparatorChar));
            if (Directory.Exists(fullPath))
            {
                return Directory.GetFiles(fullPath).Select(Path.GetFileName);
            }
            return base.GetFiles(path);
        }

        public override IEnumerable<string> GetDirectories(string path)
        {
            var fullPath = Path.Combine(baseFolder, path.Replace('\\', Path.DirectorySeparatorChar));
            if (Directory.Exists(fullPath))
            {
                return Directory.GetDirectories(fullPath).Select(Path.GetFileName);
            }
            return base.GetDirectories(path);
        }

        public override bool GetBackingFileName(string path, out string filename)
        {
            var fullPath = Path.Combine(baseFolder, path.Replace('\\', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath) || Directory.Exists(fullPath)) {
                filename = fullPath;
                return true;
            }
            if(caseSensitive)
                return base.GetBackingFileName(path, out filename);
            filename = null;
            return false;
        }

        protected override bool GetDirectoryBackingPath(VfsDirectory dir, out string filename)
        {
            filename = Path.Combine(baseFolder, GetDirectoryPath(dir));
            return true;
        }

        VfsDirectory WalkDir(string dir, VfsDirectory parent, bool recurse = true)
        {
            var d = new VfsDirectory() { Name = Path.GetFileName(dir), Parent = parent };
            foreach (var f in Directory.EnumerateFiles(dir).Select(Path.GetFileName).ToArray())
                d.Items[f] = new SysFile(Path.Combine(dir, f), f);
            var dinfo = new DirectoryInfo(dir);
            if (recurse)
            {
                foreach (var directory in dinfo.GetDirectories())
                {
                    if (!directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        d.Items[directory.Name] = WalkDir(directory.FullName, d);
                    else
                        d.Items[directory.Name] = WalkDir(directory.FullName, d, false);
                }
            }
            return d;
        }

        public override bool FileExists(string filename)
        {
            var path = Path.Combine(baseFolder, filename.Replace('\\', Path.DirectorySeparatorChar));
            if (File.Exists(path)) return true;
            if (caseSensitive)
                return base.FileExists(filename);
            else
                return false;
        }


        public override Stream Open(string filename)
        {
            var path = Path.Combine(baseFolder, filename.Replace('\\', Path.DirectorySeparatorChar));
            if (File.Exists(path)) return File.OpenRead(path);
            if (caseSensitive)
                return base.Open(filename);
            else
                return null;
        }
    }
}
