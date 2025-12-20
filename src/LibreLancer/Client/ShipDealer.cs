// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Client;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Interface;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Resources;
using LibreLancer.Server;
using LibreLancer.World;
using WattleScript.Interpreter;

namespace LibreLancer.Client
{
    [WattleScriptUserData]
    public class ShipDealer
    {
        private CGameSession session;

        public ShipDealer(CGameSession session)
        {
            this.session = session;
        }

        UISoldShip ShipInfo(Ship ship)
        {
            ship.ModelFile.LoadFile(session.Game.ResourceManager);
            return new UISoldShip()
            {
                IdsName = ship.NameIds,
                IdsInfo = ship.Infocard,
                Model = ship.ModelFile.SourcePath,
                ShipClass = ship.Class,
                Icon = session.Game.GameData.Items.GetShipIcon(ship),
                Price = session.ShipWorth,
                Ship = ship
            };
        }

        public UISoldShip PlayerShip()
        {
            if (session.PlayerShip == null) return null;
            return ShipInfo(session.PlayerShip);
        }

        public UISoldShip[] SoldShips()
        {
            return session.Ships.Select(x =>
            {
                var ship = session.Game.GameData.Items.Ships.Get(x.ShipCRC);
                var sold = ShipInfo(ship);
                sold.Server = x;
                sold.Price = x.PackagePrice;
                return sold;
            }).ToArray();
        }


        private ulong selectedHullPrice;

        class ShipTradeItem
        {
            public bool Show = true;
            public string Hardpoint;
            public NetCargo Cargo;
            public ResolvedInclude Include;
            public int Amount;
        }

        private List<ShipTradeItem> playerItems;
        private List<ShipTradeItem> dealerItems;
        private int selectedCrc;
        private Ship selectedShip;

        bool CanMount(string hpType, string hardpoint)
        {
            if(string.IsNullOrWhiteSpace(hpType)) return false;
            if (!selectedShip.PossibleHardpoints.TryGetValue(hpType, out var possible))
                return false;
            if (hardpoint != null)
            {
                if (playerItems.Any(x => hardpoint.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
                    return false;
                return possible.Any(x => x.Equals(hardpoint, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                foreach (var hp in possible)
                {
                    if (!playerItems.Any(x => hp.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }

            return false;
        }

        class ResolvedInclude
        {
            public int ID;
            public Equipment Equipment;
            public int Amount;
        }

        public void Purchase(Closure callback)
        {
            List<SellCount> sellPlayer = new List<SellCount>();
            List<SellCount> sellPackage = new List<SellCount>();
            List<MountId> mountPlayer = new List<MountId>();
            List<MountId> mountPackage = new List<MountId>();
            foreach (var item in dealerItems) {
                if (item.Cargo != null) {
                    sellPlayer.Add(new SellCount() { Count = item.Amount, ID = item.Cargo.ID });
                }
                if (item.Include != null) {
                    sellPackage.Add(new SellCount() { Count = item.Amount, ID = item.Include.ID });
                }
            }
            foreach (var item in playerItems) {
                if (item.Hardpoint != null)
                {
                    if (item.Cargo != null) {
                        mountPlayer.Add(new MountId() { Hardpoint = item.Hardpoint, ID = item.Cargo.ID });
                    }
                    if (item.Include != null) {
                        mountPackage.Add(new MountId() { Hardpoint = item.Hardpoint, ID = item.Include.ID });
                    }
                }
            }

            session.BaseRpc.PurchaseShip(selectedCrc,
                mountPlayer.ToArray(),
                mountPackage.ToArray(),
                sellPlayer.ToArray(),
                sellPackage.ToArray()).ContinueWith(task =>
            {
                string status = "fail";
                if (task.Result == ShipPurchaseStatus.Success) status = "success";
                if (task.Result == ShipPurchaseStatus.SuccessGainCredits) status = "successprofit";
                session.EnqueueAction(() => callback.Call(status));
            });
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
            List<UIInventoryItem> inventory = new List<UIInventoryItem>();
            var filterfunc = Trader.GetFilter(filter);
            foreach (var hardpoint in selectedShip.HardpointTypes)
            {
                var ui = new UIInventoryItem() {Hardpoint = hardpoint.Key, Price = -1 };
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
                var mounted = playerItems.FirstOrDefault(x =>
                    hardpoint.Key.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase));
                if (mounted != null)
                {
                    var equip = (mounted.Cargo?.Equipment ?? mounted.Include?.Equipment);
                    if (equip == null || equip.Good == null) continue;
                    ui.ID = playerItems.IndexOf(mounted);
                    ui.Count = 1;
                    ui.Good = equip.Good.Ini.Nickname;
                    ui.Icon = equip.Good.Ini.ItemIcon;
                    ui.IdsInfo = equip.IdsInfo;
                    ui.IdsName = equip.IdsName;
                    ui.Price = GetPrice(equip.Good);
                    ui.MountIcon = true;
                    ui.CanMount = true;
                    if (equip is not CommodityEquipment && mounted.Cargo != null)
                        ui.Price = (ulong) (ui.Price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
                }
                inventory.Add(ui);
            }
            for(int i = 0; i < playerItems.Count; i++)
            {
                var item = playerItems[i];
                if (!item.Show || item.Hardpoint != null) continue;
                var g = (item.Cargo?.Equipment ?? item.Include.Equipment).Good;
                if (g == null) continue;
                if (!filterfunc(g.Equipment)) continue;
                var price = GetPrice(g);
                if (g.Equipment is not CommodityEquipment && item.Cargo != null)
                    price = (ulong) (price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
                inventory.Add(new UIInventoryItem()
                {
                    ID = i,
                    Count = item.Amount,
                    Icon = g.Ini.ItemIcon,
                    Good = g.Ini.Nickname,
                    Combinable = g.Ini.Combinable,
                    IdsInfo = g.Equipment.IdsInfo,
                    IdsName = g.Equipment.IdsName,
                    MountIcon = !string.IsNullOrEmpty(g.Equipment.HpType),
                    CanMount = CanMount(g.Equipment.HpType, null),
                    Price = price
                });
            }
            Trader.SortGoods(session, inventory);
            return inventory.ToArray();
        }

        public UIInventoryItem[] GetDealerGoods(string filter)
        {
            List<UIInventoryItem> traderGoods = new List<UIInventoryItem>();
            var filterfunc = Trader.GetFilter(filter);
            for(int i = 0; i < dealerItems.Count; i++)
            {
                var item = dealerItems[i];
                if (!item.Show) continue;
                var g = (item.Cargo?.Equipment ?? item.Include.Equipment).Good;
                if (g == null) continue;
                if (!filterfunc(g.Equipment)) continue;
                var price = GetPrice(g);
                if (g.Equipment is not CommodityEquipment && item.Cargo != null)
                    price = (ulong) (price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
                traderGoods.Add(new UIInventoryItem()
                {
                    ID = i,
                    Count = item.Amount,
                    Icon = g.Ini.ItemIcon,
                    Good = g.Ini.Nickname,
                    Combinable = g.Ini.Combinable,
                    IdsInfo = g.Equipment.IdsInfo,
                    IdsName = g.Equipment.IdsName,
                    Price = price
                });
            }
            Trader.SortGoods(session, traderGoods);
            return traderGoods.ToArray();
        }

        public void TransferToPlayer(UIInventoryItem item, int count, Closure onSuccess)
        {
            var src = dealerItems[item.ID];
            if (count > src.Amount) return;
            var equip = (src.Cargo?.Equipment ?? src.Include.Equipment);
            if (equip.Good.Ini.Combinable)
            {
                ShipTradeItem dst = null;
                if (src.Cargo != null) {
                    dst = playerItems.FirstOrDefault(x => x.Cargo == src.Cargo);
                }
                else if (src.Include != null) {
                    dst = playerItems.FirstOrDefault(x => x.Cargo == src.Cargo);
                }
                if (dst != null) {
                    dst = new ShipTradeItem()
                    {
                        Cargo = src.Cargo,
                        Hardpoint = null,
                        Show = true,
                        Include = src.Include
                    };
                    playerItems.Add(dst);
                }
                dst.Amount += count;
            }
            else
            {
                playerItems.Add(new ShipTradeItem()
                {
                    Amount = 1,
                    Cargo = src.Cargo,
                    Hardpoint = null,
                    Show = true,
                    Include = src.Include
                });
            }
            src.Amount -= count;
            if (src.Amount <= 0)
                dealerItems.Remove(src);
            onSuccess.Call();
        }

        public void SellToDealer(UIInventoryItem item, int count, Closure onSuccess)
        {
            var src = playerItems[item.ID];
            if (count > src.Amount) return;
            var equip = (src.Cargo?.Equipment ?? src.Include.Equipment);
            if (equip.Good.Ini.Combinable)
            {
                ShipTradeItem dst = null;
                if (src.Cargo != null) {
                    dst = dealerItems.FirstOrDefault(x => x.Cargo == src.Cargo);
                }
                else if (src.Include != null) {
                    dst = dealerItems.FirstOrDefault(x => x.Cargo == src.Cargo);
                }
                if (dst == null) {
                    dst = new ShipTradeItem()
                    {
                        Cargo = src.Cargo,
                        Hardpoint = null,
                        Show = true,
                        Include = src.Include
                    };
                    dealerItems.Add(dst);
                }
                dst.Amount += count;
            }
            else
            {
                dealerItems.Add(new ShipTradeItem()
                {
                    Amount = 1,
                    Cargo = src.Cargo,
                    Hardpoint = null,
                    Show = true,
                    Include = src.Include
                });
            }
            src.Amount -= count;
            if (src.Amount <= 0)
                playerItems.Remove(src);
            onSuccess.Call();
        }

        public void StartPurchase(UISoldShip ship, Closure callback)
        {
            session.BaseRpc.GetShipPackage(ship.Server.PackageCRC).ContinueWith(task =>
            {
                if (task.Result != null) {
                    selectedHullPrice = ship.Server.HullPrice;
                }
                else {
                    return;
                }

                selectedCrc = ship.Server.PackageCRC;
                selectedShip = ship.Ship;
                playerItems = new List<ShipTradeItem>();
                dealerItems = new List<ShipTradeItem>();
                for (int i = 0; i < task.Result.Included.Length; i++)
                {
                    var item = task.Result.Included[i];
                    var eq = session.Game.GameData.Items.Equipment.Get(item.EquipCRC);
                    playerItems.Add(new ShipTradeItem()
                    {
                        Show = eq.Good != null,
                        Hardpoint = item.Hardpoint,
                        Include = new ResolvedInclude()
                        {
                            ID = i,
                            Equipment = eq,
                            Amount = item.Amount
                        },
                        Amount = item.Amount
                    });
                }
                foreach (var item in session.Items) {
                    if (item.Equipment.Good != null)
                    {
                        string hp = null;
                        if (item.Hardpoint != null && CanMount(item.Equipment.HpType, item.Hardpoint))
                            hp = item.Hardpoint;
                        playerItems.Add(new ShipTradeItem() { Cargo = item, Hardpoint = hp , Amount = item.Count });
                    }
                }
                session.EnqueueAction(() => callback.Call());
            });
        }

        string FirstAvailableHardpoint(string hptype)
        {
            if(string.IsNullOrWhiteSpace(hptype)) return null;
            if (!selectedShip.PossibleHardpoints.TryGetValue(hptype, out var candidates))
                return null;
            foreach (var possible in candidates)
            {
                if(!playerItems.Any(x => possible.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
                    return possible;
            }
            return null;
        }

        public double GetRequiredCredits()
        {
            var price = (long)selectedHullPrice;
            foreach (var item in dealerItems)
            {
                var eq = item.Cargo?.Equipment ?? item.Include?.Equipment;
                if (eq.Good == null) continue;
                var unitPrice = GetPrice(eq.Good);
                if (eq is not CommodityEquipment && item.Cargo != null)
                    unitPrice *= TradeConstants.EQUIP_RESALE_MULTIPLIER;
                price -= (long) unitPrice * item.Amount;
            }
            foreach (var item in playerItems)
            {
                if (item.Include?.Equipment?.Good == null) continue;
                var unitPrice = GetPrice(item.Include.Equipment.Good);
                price += (long) unitPrice * item.Amount;
            }
            price -= (long)session.ShipWorth;
            if (price > session.Credits) {
                return price - session.Credits;
            }
            return 0;
        }

        public double GetShipDisplayPrice()
        {
            var price = (long)selectedHullPrice;
            foreach (var item in dealerItems)
            {
                var eq = item.Cargo?.Equipment ?? item.Include?.Equipment;
                if (eq.Good == null) continue;
                var unitPrice = GetPrice(eq.Good);
                if (eq is not CommodityEquipment && item.Cargo != null)
                    unitPrice *= TradeConstants.EQUIP_RESALE_MULTIPLIER;
                price -= (long) unitPrice * item.Amount;
            }
            foreach (var item in playerItems)
            {
                if (item.Include?.Equipment?.Good == null) continue;
                var unitPrice = GetPrice(item.Include.Equipment.Good);
                price += (long) unitPrice * item.Amount;
            }
            return price < 0 ? 0 : price;
        }

        public void ProcessMount(UIInventoryItem item, Closure onsuccess)
        {
            if (item.Hardpoint != null)
            {
                playerItems[item.ID].Hardpoint = null;
                onsuccess.Call("unmount");
            }
            else
            {
                var x = playerItems[item.ID];
                var eq = (x.Cargo?.Equipment ?? x.Include.Equipment);
                var hp = FirstAvailableHardpoint(eq.HpType);
                if (hp != null)
                {
                    playerItems[item.ID].Hardpoint = hp;
                    onsuccess.Call("mount");
                }
            }
        }
    }
}
