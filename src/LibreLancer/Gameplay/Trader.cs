// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.GameData.Items;
using LibreLancer.Interface;
using MoonSharp.Interpreter;

namespace LibreLancer
{
    [MoonSharpUserData]
    public class Trader
    {
        private CGameSession session;
        public Trader(CGameSession session)
        {
            this.session = session;
        }

        private static Dictionary<string, Func<Equipment, bool>> filters = new();
        static bool AllowAll(Equipment equip) => true;
        static bool CommodityFilter(Equipment equip) => equip is CommodityEquipment;
        static Trader()
        {
            filters["commodity"] = CommodityFilter;
        }

        public void Buy(string good, int count)
        {
            session.RpcServer.PurchaseGood(good, count);
        }

        public void Sell(int id, int count)
        {
            session.RpcServer.SellGood(id, count);
        }

        private Closure handler;

        void UpdateAction()
        {
            
            handler.Call();
        }
        public void OnUpdateInventory(Closure handler)
        {
            this.handler = handler;
            session.OnUpdateInventory = UpdateAction;
        }
        static Func<Equipment, bool> GetFilter(string name)
        {
            if (string.IsNullOrEmpty(name)) return AllowAll;
            if (!filters.TryGetValue(name, out var func))
                return AllowAll;
            return func;
        }

        public UIInventoryItem[] GetTraderGoods(string filter)
        {
            List<UIInventoryItem> traderGoods = new List<UIInventoryItem>();
            var filterfunc = GetFilter(filter);
            foreach (var sold in session.Goods)
            {
                if (!sold.ForSale) continue;
                var nick = session.Game.GameData.GoodFromCRC(sold.GoodCRC);
                if (!session.Game.GameData.TryGetGood(nick, out ResolvedGood g))
                    continue;
                if (!filterfunc(g.Equipment)) continue;
                traderGoods.Add(new UIInventoryItem()
                {
                    ID = -1,
                    Count = 0,
                    Icon = g.Ini.ItemIcon,
                    Good = g.Ini.Nickname,
                    IdsInfo = g.Equipment.IdsInfo,
                    IdsName = g.Equipment.IdsName,
                    Price = GetPrice(g)
                });
            }
            return traderGoods.ToArray();
        }

        double GetPrice(ResolvedGood good)
        {
            foreach (var sold in session.Goods)
            {
                if (sold.GoodCRC == good.CRC) return sold.Price;
            }
            if (!session.BaselinePrices.TryGetValue(good.CRC, out ulong p))
                return good.Ini.Price;
            return p;
        }
        
        public UIInventoryItem[] GetPlayerGoods(string filter)
        {
            List<UIInventoryItem> inventoryItems = new List<UIInventoryItem>();
            var filterfunc = GetFilter(filter);
            foreach (var item in session.Cargo)
            {
                if (item.Equipment.Good == null) continue;
                if(!filterfunc(item.Equipment)) continue;
                inventoryItems.Add(new UIInventoryItem()
                {
                    ID = item.ID,
                    Count = item.Count,
                    Icon = item.Equipment.Good.Ini.ItemIcon,
                    Good = item.Equipment.Good.Ini.Nickname,
                    IdsInfo = item.Equipment.IdsInfo,
                    IdsName = item.Equipment.IdsName,
                    Price = GetPrice(item.Equipment.Good)
                });
            }
            return inventoryItems.ToArray();
        }
    }
}