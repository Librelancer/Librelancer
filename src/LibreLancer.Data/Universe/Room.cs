// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class Room : IniFile
	{
		FreelancerData GameData;
		public string Nickname { get; private set; }
		public string Music { get; private set; }
		public List<string> SceneScripts { get; private set; }
		public List<RoomHotspot> Hotspots { get; private set; }
		public string Camera { get; private set; }
		public string PlayerShipPlacement { get; private set; }
		public string LaunchingScript { get; private set; }
		public string LandingScript { get; private set; }
        public string StartScript { get; private set;  }
        public string GoodscartScript { get; private set; }
        public Room(Section section, FreelancerData data)
		{
			GameData = data;
			if (section == null) throw new ArgumentNullException("section");
			string file = null;
			SceneScripts = new List<string>();
			Hotspots = new List<RoomHotspot>();
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
            if (VFS.FileExists(data.Freelancer.DataPath + file))
            {
                foreach (Section s in ParseFile(data.Freelancer.DataPath + file))
                {
                    switch (s.Name.ToLowerInvariant())
                    {
                        case "room_info":
                            foreach (Entry e in s)
                            {
                                if (e.Name.ToLowerInvariant() == "set_script")
                                    SceneScripts.Add(e[0].ToString());
                                if (e.Name.ToLowerInvariant() == "goodscart_script")
                                    GoodscartScript = e[0].ToString();
                                if (e.Name.ToLowerInvariant() == "scene")
                                {
                                    if (e.Count == 3 || e.Count == 4)
                                        SceneScripts.Add(e[2].ToString());
                                    else
                                        SceneScripts.Add(e[1].ToString());
                                }
                            }
                            break;
                        case "room_sound":
                            foreach (Entry e in s)
                            {
                                if (e.Name.ToLowerInvariant() == "music")
                                    Music = e[0].ToString();
                            }
                            break;
                        case "camera":
                            // TODO Room camera
                            foreach (Entry e in s)
                            {
                                if (e.Name.ToLowerInvariant() == "name")
                                    Camera = e[0].ToString();
                            }
                            break;
                        case "spiels":
                            // TODO Room spiels
                            break;
                        case "playershipplacement":
                            foreach (Entry e in s)
                            {
                                switch (e.Name.ToLowerInvariant())
                                {
                                    case "name":
                                        PlayerShipPlacement = e[0].ToString();
                                        break;
                                    case "launching_script":
                                        LaunchingScript = e[0].ToString();
                                        break;
                                    case "landing_script":
                                        LandingScript = e[0].ToString();
                                        break;
                                }
                            }
                            break;
                        case "characterplacement":
                            foreach(Entry e in s)
                            {
                                switch(e.Name.ToLowerInvariant())
                                {
                                    case "start_script":
                                        StartScript = e[0].ToString();
                                        break;
                                }
                            }
                            break;
                        case "forsaleshipplacement":
                            // TODO Room forsaleshipplacement
                            break;
                        case "hotspot":
                            // TODO Room hotspot
                            var hotspot = new RoomHotspot();
                            foreach (Entry e in s)
                            {
                                switch (e.Name.ToLowerInvariant())
                                {
                                    case "name":
                                        hotspot.Name = e[0].ToString();
                                        break;
                                    case "behavior":
                                        hotspot.Behavior = e[0].ToString();
                                        break;
                                    case "room_switch":
                                        hotspot.RoomSwitch = e[0].ToString();
                                        break;
                                    case "set_virtual_room":
                                        hotspot.VirtualRoom = e[0].ToString();
                                        break;
                                }
                            }
                            Hotspots.Add(hotspot);
                            break;
                        case "flashlightset":
                            // TODO Room flashlightset
                            break;
                        case "flashlightline":
                            // TODO Room flashlightline
                            break;
                        default: throw new Exception("Invalid Section in " + file + ": " + s.Name);
                    }
                }
            }
            else
            {
                FLLog.Error("Ini", "Room file not found " + file);
            }
        }
	}
	public class RoomHotspot
	{
		public string Name;
		public string Behavior;
		public string RoomSwitch;
		public string VirtualRoom;
	}
}
