// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public abstract class ZoneReference : IniFile
	{
        [Entry("file")]
		public string IniFile;
        [Entry("zone")]
		public string ZoneName;
        [Section("texturepanels")] 
        public TexturePanelsRef TexturePanels;
        [Section("properties")]
        public List<ObjectProperties> Properties = new List<ObjectProperties>();
        [Section("exclusion zones", Delimiters = new[] { "exclude", "exclusion" })]
		public List<ExclusionZone> ExclusionZones = new List<ExclusionZone>();
        protected override void OnSelfFilled(string datapath, FileSystem vfs)
        {
            if(!string.IsNullOrEmpty(IniFile))
                ParseAndFill(datapath + IniFile, datapath, vfs);
        }
    }
}
