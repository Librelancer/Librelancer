// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
	public abstract class ZoneReference
	{
        [Entry("file")]
		public string IniFile;
        [Entry("zone")]
		public string ZoneName;
        [Section("texturepanels")]
        public TexturePanelsRef TexturePanels;
        [Section("properties")]
        public ObjectProperties Properties;
    }
}
