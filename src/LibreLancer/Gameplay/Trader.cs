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

        public void Buy(string good, int count, Closure onSuccess)
        {
            session.RpcServer.PurchaseGood(good, count).ContinueWith((x) =>
            {
                if (x.Result) session.EnqueueAction(() => onSuccess.Call());
            });
        }

        public void Sell(int id, int count, Closure onSuccess)
        {
            session.RpcServer.SellGood(id, count).ContinueWith(x =>
            {
                FLLog.Info("Client", "Sold Item!");
                if(x.Result) session.EnqueueAction(() => onSuccess.Call());
            });
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

        void SortGoods(List<UIInventoryItem> item)
        {
            //TODO: Freelancer doesn't sort alphabetically. What does it do?
            /*item.Sort((x, y) =>
            {
                var str1 = session.Game.GameData.GetString(x.IdsName) ?? "Z";
                var str2 = session.Game.GameData.GetString(y.IdsName) ?? "Z";
                return str1.CompareTo(str2);
            });*/
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
                var price = GetPrice(g);
                string rank = "neutral";
                if (g.Ini.BadBuyPrice != 0 && price >= g.Ini.BadBuyPrice * g.Ini.Price) rank = "bad";
                if (g.Ini.GoodBuyPrice != 0 && price <= g.Ini.GoodBuyPrice * g.Ini.Price) rank = "good";
                traderGoods.Add(new UIInventoryItem()
                {
                    ID = -1,
                    Count = 0,
                    Icon = g.Ini.ItemIcon,
                    Good = g.Ini.Nickname,
                    IdsInfo = g.Equipment.IdsInfo,
                    IdsName = g.Equipment.IdsName,
                    PriceRank = rank,
                    Price = price
                });
            }
            SortGoods(traderGoods);
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
                var price = GetPrice(item.Equipment.Good);
                var rank = "neutral";
                if (item.Equipment.Good.Ini.GoodSellPrice != 0 && price >= item.Equipment.Good.Ini.GoodSellPrice * item.Equipment.Good.Ini.Price) 
                    rank = "good";
                if (item.Equipment.Good.Ini.BadSellPrice != 0 && price <= item.Equipment.Good.Ini.BadSellPrice * item.Equipment.Good.Ini.Price)
                    rank = "bad";
                inventoryItems.Add(new UIInventoryItem()
                {
                    ID = item.ID,
                    Count = item.Count,
                    Icon = item.Equipment.Good.Ini.ItemIcon,
                    Good = item.Equipment.Good.Ini.Nickname,
                    IdsInfo = item.Equipment.IdsInfo,
                    IdsName = item.Equipment.IdsName,
                    Price = price,
                    PriceRank = rank
                });
            }
            SortGoods(inventoryItems);
            return inventoryItems.ToArray();
        }
    }
}