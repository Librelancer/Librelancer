// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data;
using LibreLancer.GameData.Market;

namespace LibreLancer.GameData.World
{
	public class Base
    {
        //Populated from universe
        public string Nickname;
        public int IdsName;
        public string System;
        public string TerrainTiny;
        public string TerrainSml;
        public string TerrainMdm;
        public string TerrainLrg;
        public string TerrainDyna1;
        public string TerrainDyna2;
        public string BaseRunBy;
        public bool AutosaveForbidden;
        //Populated from base ini
        public string SourceFile;
        public BaseRoom StartRoom;
        public List<BaseRoom> Rooms = new List<BaseRoom>();
        //Populated from markets
        public List<SoldShip> SoldShips = new List<SoldShip>();
        public List<BaseSoldGood> SoldGoods = new List<BaseSoldGood>();
        //Populated from mbases
        public Faction LocalFaction;
        public int Diff;
        public string MsgIdPrefix;
        public int MinMissionOffers;
        public int MaxMissionOffers; //not respected by vanilla (?)
        public List<BaseNpc> Npcs = new List<BaseNpc>();
        
		public Base()
		{
		}
	}
    
    public class BaseNpc
    {
        public string Nickname;
        public string BaseAppr;
        public string Body;
        public string Head;
        public string LeftHand;
        public string RightHand;
        public string Accessory;
        public int IndividualName;
        public Faction Affiliation;
        public string Voice;
        public string Room;
        
        public List<NpcKnow> Know = new List<NpcKnow>();
        public List<NpcRumor> Rumors = new List<NpcRumor>();
        public List<NpcBribe> Bribes = new List<NpcBribe>();
        public NpcMission Mission;
    }
}
