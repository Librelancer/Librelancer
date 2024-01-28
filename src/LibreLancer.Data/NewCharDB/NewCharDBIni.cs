// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
    
namespace LibreLancer.Data.NewCharDB
{
    public class NewCharDBIni : IniFile
    {
        [Section("faction")]
        public List<NewCharFaction> Factions = new List<NewCharFaction>();
        [Section("package")]
        public List<NewCharPackage> Packages = new List<NewCharPackage>();
        [Section("pilot")]
        public List<NewCharPilot> Pilots = new List<NewCharPilot>();

        public void AddNewCharDBIni(string path, FileSystem vfs) => ParseAndFill(path, vfs);
    }
}
