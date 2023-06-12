// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using LibreLancer.Data.Universe.Rooms;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class Room : IniFile
	{
        public string FilePath { get; private set; }
		public string Nickname { get; private set; }

        [Section("Room_Info")] 
        public RoomInfo RoomInfo;
        [Section("CharacterPlacement")] 
        public CharacterPlacement CharacterPlacement;
        [Section("Room_Sound")] 
        public RoomSound RoomSound;
        [Section("ForSaleShipPlacement")] 
        public List<NameSection> ForSaleShipPlacements = new List<NameSection>();
        [Section("Camera")] 
        public NameSection Camera;
        [Section("Hotspot")] 
        public List<RoomHotspot> Hotspots = new List<RoomHotspot>();
        [Section("PlayerShipPlacement")] 
        public PlayerShipPlacement PlayerShipPlacement;
        [Section("FlashlightSet")] 
        public List<FlashlightSet> FlashlightSets = new List<FlashlightSet>();
        [Section("FlashlightLine")] 
        public List<FlashlightLine> FlashlightLines = new List<FlashlightLine>();
        [Section("Spiels")] 
        public Spiels Spiels;
        
        public Room(Section section, FreelancerData data)
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
				case "file":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (file != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					file = e[0].ToString();
					break;
				default: throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}
            FilePath = file;
            if (data.VFS.FileExists(data.Freelancer.DataPath + file))
            {
                ParseAndFill(data.Freelancer.DataPath + file, data.VFS);
            }
            else
            {
                FLLog.Error("Ini", "Room file not found " + file);
            }
        }
	}
}
