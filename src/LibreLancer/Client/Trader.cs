// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Interface;
using LibreLancer.Server;
using LibreLancer.World;
using WattleScript.Interpreter;

namespace LibreLancer.Client
{
    [WattleScriptUserData]
    public class Trader(CGameSession session)
    {
        private static Dictionary<string, Func<Equipment, bool>> filters = new();
        private Closure? handler;

        private static bool AllowAll(Equipment equip) => true;
        private static bool CommodityFilter(Equipment equip) => equip is CommodityEquipment;

        private static bool WeaponFilter(Equipment equip)
        {
            return equip is GunEquipment or MissileLauncherEquipment or CountermeasureEquipment;
        }

        private static bool ExternalFilter(Equipment equip)
        {
            return equip is ThrusterEquipment or ShieldEquipment;
        }

        private static bool AmmoFilter(Equipment equip)
        {
            return equip is MissileEquip;
        }

        private static bool InternalFilter(Equipment equip)
        {
            return equip is ShieldBatteryEquipment or RepairKitEquipment;
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
            session.BaseRpc.PurchaseGood(good, count).ContinueWith((x) =>
            {
                if (x.Result)
                {
                    session.EnqueueAction(() => onSuccess.Call());
                }
            });
        }

        public void Sell(UIInventoryItem item, int count, Closure onSuccess)
        {
            session.BaseRpc.SellGood(item.ID, count).ContinueWith(x =>
                {
                    FLLog.Info("Client", "Sold Item!");
                    if(x.Result)
                    {
                        session.EnqueueAction(() => onSuccess.Call());
                    }
                });
        }

        private void UpdateAction()
        {
            handler?.Call();
        }

        public void OnUpdateInventory(Closure handler)
        {
            this.handler = handler;
            session.OnUpdateInventory = UpdateAction;
        }

        public static Func<Equipment, bool> GetFilter(string name)
        {
            if (string.IsNullOrEmpty(name) || !filters.TryGetValue(name, out var func))
            {
                return AllowAll;
            }

            return func;
        }

        public static void SortGoods(CGameSession session, List<UIInventoryItem> item)
        {
            item.Sort((x, y) =>
            {
                if (x.Hardpoint != null && y.Hardpoint == null)
                {
                    return -1;
                }

                if (y.Hardpoint != null && x.Hardpoint == null)
                {
                    return 1;
                }

                if (x.Hardpoint != null && y.Hardpoint != null)
                {
                    var comp = x.HpSortIndex.CompareTo(y.HpSortIndex);
                    return comp == 0 ? string.CompareOrdinal(x.Hardpoint, y.Hardpoint) : comp;
                }

                var str1 = session.Game.GameData.GetString(x.IdsName) ?? "Z";
                var str2 = session.Game.GameData.GetString(y.IdsName) ?? "Z";
                return string.Compare(str1, str2, StringComparison.Ordinal);
            });
        }

        public void ProcessMount(UIInventoryItem item, Closure onsuccess)
        {
            if (item.Hardpoint != null)
            {
                session.BaseRpc.Unmount(item.Hardpoint).ContinueWith((x) =>
                {
                    if(x.Result)
                    {
                        session.EnqueueAction(() => onsuccess.Call("unmount"));
                    }
                });
            }
            else
            {
                session.BaseRpc.Mount(item.ID).ContinueWith((x) =>
                {
                    if(x.Result)
                    {
                        session.EnqueueAction(() => onsuccess.Call("mount"));
                    }
                });
            }
        }

        public UIInventoryItem[] GetTraderGoods(string filter)
        {
            List<UIInventoryItem> traderGoods = [];
            var filterFunc = GetFilter(filter);
            foreach (var sold in session.Goods)
            {
                if (!sold.ForSale)
                {
                    continue;
                }

                if (!session.Game.GameData.Items.Goods.TryGetValue(sold.GoodCRC, out var g))
                {
                    continue;
                }

                if (!filterFunc(g.Equipment))
                {
                    continue;
                }

                var price = GetPrice(g);
                var rank = "neutral";
                if (g.Ini.BadBuyPrice != 0 && price >= g.Ini.BadBuyPrice * g.Ini.Price)
                {
                    rank = "bad";
                }

                if (g.Ini.GoodBuyPrice != 0 && price <= g.Ini.GoodBuyPrice * g.Ini.Price)
                {
                    rank = "good";
                }

                if (g.Ini.BadBuyPrice == 0 && g.Ini.GoodBuyPrice == 0)
                {
                    rank = null;
                }

                traderGoods.Add(new UIInventoryItem()
                {
                    ID = -1,
                    Count = 0,
                    Icon = g.Ini.ItemIcon,
                    Good = g.Ini.Nickname,
                    Combinable = g.Ini.Combinable,
                    IdsInfo = g.Equipment.IdsInfo,
                    IdsName = g.Equipment.IdsName,
                    Volume = g.Equipment.Volume,
                    PriceRank = rank,
                    Price = price,
                    Equipment = g.Equipment
                });
            }

            SortGoods(session, traderGoods);
            return traderGoods.ToArray();
        }

        private double GetPrice(ResolvedGood good)
        {
            foreach (var sold in session.Goods)
            {
                if (sold.GoodCRC == good.CRC)
                {
                    return sold.Price;
                }
            }
            if (!session.BaselinePrices.TryGetValue(good.CRC, out var p))
            {
                return good.Ini.Price;
            }

            return p;
        }

        private bool CanMount(string? hpType)
        {
            if(string.IsNullOrWhiteSpace(hpType) || session.PlayerShip == null)
            {
                return false;
            }

            if (!session.PlayerShip.PossibleHardpoints.TryGetValue(hpType, out var possible))
            {
                return false;
            }

            foreach (var hp in possible)
            {
                if (!session.Items.Any(x => hp.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        public static UIInventoryItem FromNetCargo(NetCargo item, double price, bool canMount)
        {
            var rank = "neutral";
            if (item.Equipment!.Good!.Ini.GoodSellPrice != 0 && price >= item.Equipment.Good.Ini.GoodSellPrice * item.Equipment.Good.Ini.Price)
            {
                rank = "good";
            }

            if (item.Equipment.Good.Ini.BadSellPrice != 0 && price <= item.Equipment.Good.Ini.BadSellPrice * item.Equipment.Good.Ini.Price)
            {
                rank = "bad";
            }

            if (item.Equipment.Good.Ini.BadSellPrice == 0 && item.Equipment.Good.Ini.GoodSellPrice == 0)
            {
                rank = null;
            }

            return new UIInventoryItem()
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
                Volume = item.Equipment.Volume,
                Combinable = item.Equipment.Good.Ini.Combinable,
                CanMount = canMount,
                Equipment = item.Equipment
            };
        }

        public int GetPurchaseLimit(UIInventoryItem item)
        {
            var maxAmount = (int) Math.Floor(session.Credits / item.Price);
            var holdLimit = CargoUtilities.GetItemLimit(session.Items, session.PlayerShip!, item.Equipment!);
            return Math.Min(maxAmount, holdLimit);
        }

        public float GetHoldSize() => session.PlayerShip!.HoldSize;

        public float GetUsedHoldSpace() => session.Items.Select(x => x.Count * x.Equipment!.Volume).Sum();

        public UIInventoryItem[] GetPlayerGoods(string filter)
        {
            if (session.PlayerShip == null)
            {
                return [];
            }

            List<UIInventoryItem> inventoryItems = [];
            var filterfunc = GetFilter(filter);
            if (session.PlayerShip != null)
            {
                foreach (var hardpoint in session.PlayerShip.HardpointTypes)
                {
                    var ui = new UIInventoryItem() {Hardpoint = hardpoint.Key};
                    var hptype = hardpoint.Value.OrderByDescending(x => x.Class).First();
                    switch (filter.ToLowerInvariant())
                    {
                        case "commodity":
                        case "ammo":
                            continue;
                        case "weapons":
                            if (hptype.Category != HpCategory.Weapon)
                            {
                                continue;
                            }

                            break;
                        case "internal":
                            if (hptype.Category != HpCategory.Internal)
                            {
                                continue;
                            }

                            break;
                        case "external":
                            if (hptype.Category != HpCategory.External)
                            {
                                continue;
                            }

                            break;
                    }

                    ui.IdsHardpoint = hptype.IdsName;
                    ui.HpSortIndex = hptype.SortIndex;
                    ui.IdsHardpointDescription = hptype.IdsHpDescription;
                    var mounted = session.Items.FirstOrDefault(x =>
                        hardpoint.Key.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase));
                    if (mounted != null)
                    {
                        var equip = mounted.Equipment;
                        if (equip?.Good == null)
                        {
                            continue;
                        }

                        ui.ID = mounted.ID;
                        ui.Count = 1;
                        ui.Good = equip.Good.Ini.Nickname;
                        ui.Icon = equip.Good.Ini.ItemIcon;
                        ui.IdsInfo = equip.IdsInfo;
                        ui.IdsName = equip.IdsName;
                        ui.Volume = equip.Volume;
                        ui.Equipment = equip;
                        ui.Price = GetPrice(equip.Good);
                        ui.MountIcon = true;
                        ui.CanMount = true;
                        if (equip is not CommodityEquipment)
                        {
                            ui.Price = (ulong) (ui.Price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
                        }
                    }

                    inventoryItems.Add(ui);
                }
            }
            foreach (var item in session.Items)
            {
                if (item.Equipment?.Good == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(item.Hardpoint))
                {
                    continue;
                }

                if(!filterfunc(item.Equipment))
                {
                    continue;
                }

                var price = GetPrice(item.Equipment.Good);
                if (item.Equipment is not CommodityEquipment)
                {
                    price = (ulong) (price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
                }

                inventoryItems.Add(FromNetCargo(item, price, CanMount(item.Equipment.HpType)));
            }
            SortGoods(session, inventoryItems);
            return inventoryItems.ToArray();
        }
    }
}
