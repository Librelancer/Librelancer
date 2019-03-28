// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Goods
{
    public class Good
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("msg_id_prefix")]
        public string MsgIdPrefix;
        [Entry("equipment")]
        public string Equipment;
        [Entry("ship")]
        public string Ship;
        [Entry("hull")]
        public string Hull;
        [Entry("ids_name")]
        public int IdsName;
        [Entry("ids_info")]
        public int IdsInfo;
        [Entry("shop_archetype")]
        public string ShopArchetype;
        [Entry("da_archetype")]
        public string DaArchetype; //Is this valid? Probably same as ShopArchetype
        [Entry("attachment_archetype")]
        public string AttachmentArchetype; //TODO: Sort these archetypes out
        [Entry("material_library")]
        public List<string> MaterialLibraries = new List<string>();
        [Entry("price")]
        public int Price;
        [Entry("combinable")]
        public bool Combinable;
        [Entry("good_sell_price")]
        public float GoodSellPrice;
        [Entry("bad_buy_price")]
        public float BadBuyPrice;
        [Entry("bad_sell_price")]
        public float BadSellPrice;
        [Entry("good_buy_price")]
        public float GoodBuyPrice;
        [Entry("jump_dist")]
        public int JumpDist;
        [Entry("item_icon")]
        public string ItemIcon;
        [Entry("category")]
        public GoodCategory Category;

        //Manual
        public List<GoodAddon> Addons = new List<GoodAddon>();
        public string FreeAmmoName;
        public int FreeAmmoCount;

        bool HandleEntry(Entry e)
        {
            if(e.Name.Equals("addon",StringComparison.InvariantCultureIgnoreCase))
            {
                int amt = 1;
                if (e.Count > 2) amt = e[2].ToInt32();
                Addons.Add(new GoodAddon() { Equipment = e[0].ToString(), Hardpoint = e[1].ToString(), Amount = amt });
                return true;
            } else if (e.Name.Equals("free_ammo", StringComparison.InvariantCultureIgnoreCase))
            {
                FreeAmmoName = e[0].ToString();
                FreeAmmoCount = e[1].ToInt32();
                return true;
            }
            return false;
        }
    }
    public class GoodAddon
    {
        public string Equipment;
        public string Hardpoint;
        public int Amount; //Check if this is correct?
    }
}
