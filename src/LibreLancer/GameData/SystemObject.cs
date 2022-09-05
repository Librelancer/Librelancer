// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.GameData.Items;

namespace LibreLancer.GameData
{
	public class SystemObject
	{
		public string Nickname;
		public string DisplayName;
        public int[] IdsInfo;
        public string Base; //used for linking IdsInfo
		public Archetype Archetype;
		public Vector3 Position = Vector3.Zero;
        public Vector3 Spin = Vector3.Zero;
		public Matrix4x4? Rotation;
		public Dictionary<string, Equipment> Loadout = new Dictionary<string, Equipment>();
		public List<Equipment> LoadoutNoHardpoint = new List<Equipment>();
		public DockAction Dock;
        public Faction Faction;
        public int Visit;
        
        public SystemObject ()
		{
		}
	}
}

