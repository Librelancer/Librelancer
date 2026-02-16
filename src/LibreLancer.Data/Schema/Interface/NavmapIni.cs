// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Interface;

//This class is a librelancer extension.
[ParsedIni]
public partial class NavmapIni
{
    [Section("LibraryFiles")]
    public List<LibraryFiles> LibraryFiles = [];

    [Section("IconType")]
    public NavmapIniIconType? Type;

    [Section("Icons")]
    public NavmapIniIcons? Icons;

    [Section("Background")]
    public NavmapIniBackground? Background;

    public NavmapIni(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        ParseIni(path, vfs, stringPool);
    }
}

[ParsedSection]
public partial class LibraryFiles
{
    [Entry("file", Multiline = true)] public List<string> Files = [];
}

public enum NavIconType
{
    Model,
    Texture
}

[ParsedSection]
public partial class NavmapIniIconType
{
    [Entry("type")] public NavIconType Type;
}

[ParsedSection]
public partial class NavmapIniBackground
{
    [Entry("texture")] public string? Texture;
}

[ParsedSection]
public partial class NavmapIniIcons : IEntryHandler
{
    public Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase);

    bool IEntryHandler.HandleEntry(Entry e)
    {
        Map[e.Name] = e[0].ToString();
        return true;
    }
}
