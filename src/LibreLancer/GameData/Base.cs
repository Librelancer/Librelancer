// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.GameData.Market;
    
namespace LibreLancer.GameData
{
	public class Base
    {
        public string Nickname;
        public int IdsName;
        public string System;
		public BaseRoom StartRoom;
		public List<BaseRoom> Rooms = new List<BaseRoom>();
        public List<SoldShip> SoldShips = new List<SoldShip>();
        
        public string TerrainTiny;
        public string TerrainSml;
        public string TerrainMdm;
        public string TerrainLrg;
        public string TerrainDyna1;
        public string TerrainDyna2;
        
		public Base()
		{
		}
	}
}
