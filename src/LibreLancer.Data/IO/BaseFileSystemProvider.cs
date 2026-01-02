using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibreLancer.Data.IO;

public abstract class BaseFileSystemProvider : IFileProvider
{
    protected VfsDirectory Root { get; set; } = null!;

    public virtual Stream? Open(string filename)
    {
        var it = GetItem(filename);
        return it is VfsFile file
            ? file.OpenRead()
            : null;
    }

    public virtual IEnumerable<string> GetFiles(string path)
    {
        var it = GetItem(path);
        return (it is not VfsDirectory dir
            ? Array.Empty<string>()
            : dir.Items.Values.Where(x => x is VfsFile).Select(x => x.Name))!;
    }

    public virtual IEnumerable<string> GetDirectories(string path)
    {
        var it = GetItem(path);
        return (it is not VfsDirectory dir
            ? Array.Empty<string>()
            : dir.Items.Values.Where(x => x is VfsDirectory).Select(x => x.Name))!;
    }

    public virtual bool GetBackingFileName(string path, out string? fileName)
    {
        var it = GetItem(path);
        switch (it)
        {
            case VfsFile file:
                fileName = file.GetBackingFilename();
                return true;
            case VfsDirectory dir:
                return GetDirectoryBackingPath(dir, out fileName);
            default:
                fileName = null;
                return false;
        }
    }

    protected string GetDirectoryPath(VfsDirectory dir)
    {
        if (dir.Parent == null)
        {
            return "";
        }

        List<string> components = [];
        var d = dir;
        while (d is { Parent: not null, Name: not null })
        {
            components.Add(d.Name);
            d = d.Parent;
        }

        components.Reverse();
        return string.Join('/', components);
    }

    protected virtual bool GetDirectoryBackingPath(VfsDirectory dir, out string? fileName)
    {
        fileName = null;
        return false;
    }

    protected VfsItem? GetItem(string filename)
    {
        var split = filename.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        VfsDirectory current = Root;
        for (int i = 0; i < split.Length; i++)
        {
            switch (split[i])
            {
                case ".":
                    continue;
                case ".." when current.Parent == null:
                    return null;
                case "..":
                    current = current.Parent;
                    continue;
            }

            if (!current.Items.TryGetValue(split[i], out var item))
            {
                return null;
            }

            switch (item)
            {
                case VfsDirectory dir:
                    current = dir;
                    break;
                case VfsFile when i == split.Length - 1:
                    return item;
                case VfsFile:
                    return null;
            }
        }
        return current;
    }

    public virtual bool FileExists(string filename) => GetItem(filename) is VfsFile;

    public abstract void Refresh();
}
