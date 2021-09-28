// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using MoonSharp.Interpreter;

namespace LibreLancer.Interface
{
    [MoonSharpUserData]
    public class UIInventoryItem
    {
        public int ID; //id - for inventory
        public string Good; //good nickname - for selling
        public string Icon; //3db file - from good
        public int IdsName; //from item def
        public int IdsInfo;
        public double Price; //price per unit
        public string PriceRank;
        public string Hardpoint;
        public int IdsHardpoint;
        public int IdsHardpointDescription;
        public bool Combinable;
        public int Count;  //how many do we have in this slot? (set to 0 to not show count)
        public bool MountIcon;
        public bool CanMount;
        internal int HpSortIndex;
    }
}