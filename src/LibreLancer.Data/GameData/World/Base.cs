// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.Market;
using LibreLancer.Data.Schema.MBases;

namespace LibreLancer.Data.GameData.World
{
	public class Base : IdentifiableItem
    {
        //Populated from universe
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
        public GameItemCollection<BaseRoom> Rooms = new GameItemCollection<BaseRoom>();
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

        public ulong GetUnitPrice(Items.Equipment eq)
        {
            var g = SoldGoods.FirstOrDefault(x =>
                x.Good.Equipment.Nickname.Equals(eq.Nickname, StringComparison.OrdinalIgnoreCase));
            if (g.Good == null) {
                return (ulong) (eq.Good?.Ini?.Price ?? 0);
            }
            return g.Price;
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
