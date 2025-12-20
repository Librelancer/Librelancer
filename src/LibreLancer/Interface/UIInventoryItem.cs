// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.GameData.Items;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    public class UIInventoryItem
    {
        public int ID; //id - for inventory
        public string Good; //good nickname - for selling
        public string Icon; //3db file - from good
        public int IdsName; //from item def
        public int IdsInfo;
        public double Price; //price per unit
        public double Volume; //cargo volume per unit
        public string PriceRank;
        public string Hardpoint;
        public int IdsHardpoint;
        public int IdsHardpointDescription;
        public bool Combinable;
        public int Count;  //how many do we have in this slot? (set to 0 to not show count)
        public bool MountIcon;
        public bool CanMount;
        internal int HpSortIndex;

        [WattleScriptHidden]
        public Equipment Equipment;
    }
}
