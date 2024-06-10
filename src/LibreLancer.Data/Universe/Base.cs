// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
    //TODO: Update to new Ini API
	public class Base : UniverseElement
	{
		public string System { get; private set; }
		public string BGCSBaseRunBy { get; private set; }
		public string TerrainTiny { get; private set; }
		public string TerrainSml { get; private set; }
		public string TerrainMdm { get; private set; }
		public string TerrainLrg { get; private set; }
		public string TerrainDyna1 { get; private set; }
		public string TerrainDyna2 { get; private set; }
		public bool? AutosaveForbidden { get; private set; }

		public string StartRoom { get; private set; }

		public List<Room> Rooms { get; private set; }

        public string SourceFile { get; private set; }

		public Base(Section section, FreelancerData data) : base(data)
		{
			if (section == null) throw new ArgumentNullException("section");
			string file = null;
			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Nickname = e[0].ToString();
						break;
					case "system":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (System != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						System = e[0].ToString();
						break;
					case "strid_name":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (IdsName != 0) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
                        IdsName = e[0].ToInt32();
						break;
					case "file":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (file != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						file = e[0].ToString();
						break;
					case "bgcs_base_run_by":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (BGCSBaseRunBy != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						BGCSBaseRunBy = e[0].ToString();
						break;
					case "terrain_tiny":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (TerrainTiny != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						TerrainTiny = e[0].ToString();
						break;
					case "terrain_sml":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (TerrainSml != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						TerrainSml = e[0].ToString();
						break;
					case "terrain_mdm":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (TerrainMdm != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						TerrainMdm = e[0].ToString();
						break;
					case "terrain_lrg":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (TerrainLrg != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						TerrainLrg = e[0].ToString();
						break;
					case "terrain_dyna_01":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (TerrainDyna1 != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						TerrainDyna1 = e[0].ToString();
						break;
					case "terrain_dyna_02":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (TerrainDyna2 != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						TerrainDyna2 = e[0].ToString();
						break;
					case "autosave_forbidden":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (AutosaveForbidden != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						AutosaveForbidden = e[0].ToBoolean();
						break;
					default:
                        FLLog.Warning("Ini", $"Invalid Entry `{e.Name}` in {section.Name}: {e.Section.File}:{e.Line}");
                        break;
				}
			}

			Rooms = new List<Room>();
            SourceFile = file;

            if (data.VFS.FileExists(data.Freelancer.DataPath + file))
            {
                foreach (Section s in ParseFile(data.Freelancer.DataPath + file, data.VFS))
                {
                    switch (s.Name.ToLowerInvariant())
                    {
                        case "baseinfo":
                            foreach (Entry e in s)
                            {
                                switch (e.Name.ToLowerInvariant())
                                {
                                    case "nickname":
                                        if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
                                        if (Name != null) FLLog.Warning("Base", "Duplicate " + e.Name + " Entry in " + s.Name);
                                        Name = e[0].ToString();
                                        break;
                                    case "start_room":
                                        if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
                                        if (StartRoom != null) FLLog.Warning("Base", "Duplicate " + e.Name + " Entry in " + s.Name);
                                        StartRoom = e[0].ToString();
                                        break;
                                    case "price_variance":
                                        FLLog.Error("Base", "Unimplemented: price_variance");
                                        break;
                                    default:
                                        FLLog.Warning("Ini", $"Invalid Entry `{e.Name}` in {section.Name}: {section.File}:{e.Line}");
                                        break;
                                }
                            }
                            break;
                        case "room":
                            Rooms.Add(new Room(s, data));
                            break;
                        default:
                            FLLog.Warning("Ini", $"Invalid Sectiom `{s.Name}` in {s.File}:{s.Line}");
                            break;
                    }
                }
            }
            else
            {
                FLLog.Error("Ini", "Base ini could not find file " + file);
            }


        }
	}
}
