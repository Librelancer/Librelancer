using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibreLancer.Data.IO;

public abstract class BaseFileSystemProvider : IFileProvider
{
    protected VfsDirectory Root { get; set; }

    public virtual Stream Open(string filename)
    {
        var it = GetItem(filename);
        if (it is VfsFile file)
            return file.OpenRead();
        else
            return null;
    }

    public virtual IEnumerable<string> GetFiles(string path)
    {
        var it = GetItem(path);
        if (it is not VfsDirectory dir)
            return Array.Empty<string>();
        return dir.Items.Values.Where(x => x is VfsFile).Select(x => x.Name);
    }

    public virtual IEnumerable<string> GetDirectories(string path)
    {
        var it = GetItem(path);
        if (it is not VfsDirectory dir)
            return Array.Empty<string>();
        return dir.Items.Values.Where(x => x is VfsDirectory).Select(x => x.Name);
    }

    public virtual bool GetBackingFileName(string path, out string filename)
    {
        var it = GetItem(path);
        if (it is VfsFile file)
        {
            filename = file.GetBackingFilename();
            return true;
        }
        else if (it is VfsDirectory dir)
        {
            return GetDirectoryBackingPath(dir, out filename);
        }
        else
        {
            filename = null;
            return false;
        }
    }

    protected string GetDirectoryPath(VfsDirectory dir)
    {
        if (dir.Parent == null)
            return "";
        List<string> components = new List<string>();
        var d = dir;
        while (d?.Parent != null) {
            components.Add(d.Name);
            d = d.Parent;
        }
        components.Reverse();
        return string.Join('/', components);
    }

    protected virtual bool GetDirectoryBackingPath(VfsDirectory dir, out string filename)
    {
        filename = null;
        return false;
    }

    protected VfsItem GetItem(string filename)
    {
        var split = filename.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        VfsDirectory current = Root;
        for (int i = 0; i < split.Length; i++)
        {
            if (split[i] == ".")
                continue;
            if (split[i] == "..")
            {
                if (current.Parent == null)
                    return null;
                current = current.Parent;
                continue;
            }
            if (!current.Items.TryGetValue(split[i], out var item))
                return null;
            if (item is VfsDirectory dir)
                current = dir;
            else if (item is VfsFile)
            {
                if (i == split.Length - 1)
                    return item;
                else
                    return null;
            }
        }
        return current;
    }

    public virtual bool FileExists(string filename) => GetItem(filename) is VfsFile;

    public abstract void Refresh();
}
