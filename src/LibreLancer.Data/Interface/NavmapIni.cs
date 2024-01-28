// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Interface
{
    //This class is a librelancer extension.
    [SelfSection("LibraryFiles")]
    public class NavmapIni : IniFile
    {
        [Entry("file", Multiline = true)]
        public List<string> LibraryFiles = new List<string>();
        
        [Section("IconType")] 
        public NavmapIniIconType Type;
        
        [Section("Icons")]
        public NavmapIniIcons Icons;
        
        [Section("Background")]
        public NavmapIniBackground Background;

        public NavmapIni(string path, FileSystem vfs)
        {
            ParseAndFill(path, vfs);
        }
    }

    public enum NavIconType
    {
        Model,
        Texture
    }
    
    public class NavmapIniIconType
    {
        [Entry("type")] public NavIconType Type;
    }

    public class NavmapIniBackground
    {
        [Entry("texture")] public string Texture;
    }
    public class NavmapIniIcons : IEntryHandler
    {
        public Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        bool IEntryHandler.HandleEntry(Entry e)
        {
            Map[e.Name] = e[0].ToString();
            return true;
        }
    }
}