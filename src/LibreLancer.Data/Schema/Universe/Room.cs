// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Universe.Rooms;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
    [ParsedIni]
	public partial class Room
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("file")]
        public string File;

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

        [OnParseDependent]
        void ParseDependent(IniStringPool stringPool, IniParseProperties properties)
        {
            if (string.IsNullOrWhiteSpace(File)) return;
            if (properties["vfs"] is not FileSystem vfs) return;
            if (properties["dataPath"] is not string dataPath) return;
            ParseIni(dataPath + File, vfs, stringPool, properties);
        }
	}
}
