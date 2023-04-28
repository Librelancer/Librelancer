// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using System.Text;
using LibreLancer.Data;
using LibreLancer.GameData.Archetypes;
using LibreLancer.GameData.Items;

namespace LibreLancer.GameData.World
{
	public class SystemObject
	{
		public string Nickname;
        public int IdsName;
        public int[] IdsInfo;
        public string Base; //used for linking IdsInfo
		public Archetype Archetype;
		public Vector3 Position = Vector3.Zero;
        public Vector3 Spin = Vector3.Zero;
		public Matrix4x4? Rotation;
        public ObjectLoadout Loadout;
		public DockAction Dock;
        public Faction Reputation;
        public VisitFlags Visit;

        public int TradelaneSpaceName;
        public int IdsLeft;
        public int IdsRight;
        
        //Properties not yet used in game, but copied from ini for round trip support
        public Pilot Pilot;
        public Faction Faction;
        public int DifficultyLevel;
        public string Behavior; //Unused? Always NOTHING in vanilla
        public float AtmosphereRange;
        public string MsgIdPrefix;
        public Color4? BurnColor;
        public Color4? AmbientColor;
        public string Parent;
        
        public SystemObject ()
		{
		}
    }
}

