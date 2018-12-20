// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public abstract class ZoneReference : IniFile
	{
		protected StarSystem parent;
		protected string file;

		public string ZoneName;
		private Zone zone;
		public Zone Zone
		{
			get
			{
				if (zone == null) zone = parent.FindZone(ZoneName);
				return zone;
			}
		}

		public TexturePanelsRef TexturePanels { get; protected set; }
		public List<string> Properties { get; private set; }
		public List<ExclusionZone> ExclusionZones { get; private set; }
		protected FreelancerData GameData;

		protected ZoneReference(StarSystem parent, Section section, FreelancerData data)
		{
			if (parent == null) throw new ArgumentNullException("parent");
			if (section == null) throw new ArgumentNullException("section");
			GameData = data;
			this.parent = parent;

			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "file":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (file != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					file = e[0].ToString();
					break;
				case "zone":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (ZoneName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					ZoneName = e[0].ToString();
					break;
				default:
					throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}

			Properties = new List<string>();
			ExclusionZones = new List<ExclusionZone>();
		}
	}
}
