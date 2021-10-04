// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Equipment;
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
        static bool WeaponFilter(Equipment equip)
        {
            return equip is GunEquipment ||
                   equip is CountermeasureEquipment;
        }

        static bool ExternalFilter(Equipment equip)
        {
            return equip is ThrusterEquipment ||
                   equip is ShieldEquipment;
        }

        static bool AmmoFilter(Equipment equip)
        {
            return false;
        }

        static bool InternalFilter(Equipment equip)
        {
            return false;
        }
        static Trader()
        {
            filters["commodity"] = CommodityFilter;
            filters["weapons"] = WeaponFilter;
            filters["ammo"] = AmmoFilter;
            filters["external"] = ExternalFilter;
            filters["internal"] = InternalFilter;
        }

        public void Buy(string good, int count, Closure onSuccess)
        {
            session.RpcServer.PurchaseGood(good, count).ContinueWith((x) =>
            {
                if (x.Result) session.EnqueueAction(() => onSuccess.Call());
            });
        }

        public void Sell(UIInventoryItem item, int count, Closure onSuccess)
        {
            if (item.Hardpoint != null)
            {
                session.RpcServer.SellAttachedGood(item.Hardpoint).ContinueWith(x =>
                {
                    FLLog.Info("Client", "Sold Item!");
                    if(x.Result) session.EnqueueAction(() => onSuccess.Call());
                });
            }
            else
            {
                session.RpcServer.SellGood(item.ID, count).ContinueWith(x =>
                {
                    FLLog.Info("Client", "Sold Item!");
                    if(x.Result) session.EnqueueAction(() => onSuccess.Call());
                });
            }
           
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
            item.Sort((x, y) =>
            {
                if (x.Hardpoint != null && y.Hardpoint == null)
                    return -1;
                if (y.Hardpoint != null && x.Hardpoint == null)
                    return 1;
                if (x.Hardpoint != null && y.Hardpoint != null)
                {
                    int comp = x.HpSortIndex.CompareTo(y.HpSortIndex);
                    if (comp == 0) return string.CompareOrdinal(x.Hardpoint, y.Hardpoint);
                    else return comp;
                }
                var str1 = session.Game.GameData.GetString(x.IdsName) ?? "Z";
                var str2 = session.Game.GameData.GetString(y.IdsName) ?? "Z";
                return str1.CompareTo(str2);
            });
        }

        public void ProcessMount(UIInventoryItem item, Closure onsuccess)
        {
            if (item.Hardpoint != null)
            {
                session.RpcServer.Unmount(item.Hardpoint).ContinueWith((x) =>
                {
                    if(x.Result) session.EnqueueAction(() => onsuccess.Call("unmount"));
                });
            }
            else
            {
                session.RpcServer.Mount(item.ID).ContinueWith((x) =>
                {
                    if(x.Result) session.EnqueueAction(() => onsuccess.Call("mount"));
                });
            }
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
                if (g.Ini.BadBuyPrice == 0 && g.Ini.GoodBuyPrice == 0) rank = null;
                traderGoods.Add(new UIInventoryItem()
                {
                    ID = -1,
                    Count = 0,
                    Icon = g.Ini.ItemIcon,
                    Good = g.Ini.Nickname,
                    Combinable = g.Ini.Combinable,
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

        bool CanMount(string hpType)
        {
            if(string.IsNullOrWhiteSpace(hpType)) return false;
            var myShip = session.Game.GameData.GetShip(session.PlayerShip);
            if (!myShip.PossibleHardpoints.TryGetValue(hpType, out var possible))
                return false;
            foreach (var hp in possible)
            {
                if (!session.Mounts.Any(x => hp.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }
        
        public UIInventoryItem[] GetPlayerGoods(string filter)
        {
            List<UIInventoryItem> inventoryItems = new List<UIInventoryItem>();
            var filterfunc = GetFilter(filter);
            var myShip = session.Game.GameData.GetShip(session.PlayerShip);
            foreach (var hardpoint in myShip.HardpointTypes)
            {
                var ui = new UIInventoryItem() {Hardpoint = hardpoint.Key};
                var hptype = hardpoint.Value.OrderByDescending(x => x.Class).First();
                switch (filter.ToLowerInvariant()) {
                    case "commodity":
                    case "ammo":
                        continue;
                    case "weapons":
                        if (hptype.Category != HpCategory.Weapon) continue;
                        break;
                    case "internal":
                        if (hptype.Category != HpCategory.Internal) continue;
                        break;
                    case "external":
                        if (hptype.Category != HpCategory.External) continue;
                        break;
                }
                ui.IdsHardpoint = hptype.IdsName;
                ui.HpSortIndex = hptype.SortIndex;
                ui.IdsHardpointDescription = hptype.IdsHpDescription;
                var mounted = session.Mounts.FirstOrDefault(x =>
                    hardpoint.Key.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase));
                if (mounted != null)
                {
                    var equip = session.Game.GameData.GetEquipment(mounted.Item);
                    if (equip == null || equip.Good == null) continue;
                    ui.Count = 1;
                    ui.Good = equip.Good.Ini.Nickname;
                    ui.Icon = equip.Good.Ini.ItemIcon;
                    ui.IdsInfo = equip.IdsInfo;
                    ui.IdsName = equip.IdsName;
                    ui.Price = GetPrice(equip.Good);
                    ui.MountIcon = true;
                    ui.CanMount = true;
                }
                inventoryItems.Add(ui);
            }
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
                if (item.Equipment.Good.Ini.BadSellPrice == 0 && item.Equipment.Good.Ini.GoodSellPrice == 0) rank = null;
                inventoryItems.Add(new UIInventoryItem()
                {
                    ID = item.ID,
                    Count = item.Count,
                    Icon = item.Equipment.Good.Ini.ItemIcon,
                    Good = item.Equipment.Good.Ini.Nickname,
                    IdsInfo = item.Equipment.IdsInfo,
                    IdsName = item.Equipment.IdsName,
                    Price = price,
                    PriceRank = rank,
                    MountIcon = !string.IsNullOrEmpty(item.Equipment.HpType),
                    Combinable = item.Equipment.Good.Ini.Combinable,
                    CanMount = CanMount(item.Equipment.HpType)
                });
            }
            SortGoods(inventoryItems);
            return inventoryItems.ToArray();
        }
    }
}