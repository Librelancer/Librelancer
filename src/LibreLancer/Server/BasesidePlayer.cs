using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.Market;
using LibreLancer.Data.GameData.World;
using LibreLancer.Entities.Character;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Server;

public class BasesidePlayer : IBasesidePlayer
{
    public Player Player;
    public Base BaseData;

    public BasesidePlayer(Player player, Base baseData)
    {
        BaseData = baseData;
        Player = player;
    }

    string FirstAvailableHardpoint(string hptype)
    {
        if (string.IsNullOrWhiteSpace(hptype)) return null;
        if (!Player.Character.Ship.PossibleHardpoints.TryGetValue(hptype, out var candidates))
            return null;
        int currIndex = int.MaxValue;
        string currValue = null;
        foreach (var possible in candidates)
        {
            if (!Player.Character.Items.Any(x => possible.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
            {
                var index = Player.Character.Ship.HardpointTypes[possible].OrderBy(x => x.SortIndex)
                    .FirstOrDefault().SortIndex;
                if (index < currIndex)
                {
                    currIndex = index;
                    currValue = possible;
                }
            }
        }

        return currValue;
    }

    public Task<bool> PurchaseGood(string item, int count)
    {
        if (BaseData == null) return Task.FromResult(false);
        var g = BaseData.SoldGoods.FirstOrDefault(x =>
            x.Good.Equipment.Nickname.Equals(item, StringComparison.OrdinalIgnoreCase));
        if (g.Good == null) return Task.FromResult(false);
        var cost = (long) (g.Price * (ulong) count);
        if (Player.Character.Credits >= cost)
        {
            string hp = count == 1
                ? FirstAvailableHardpoint(g.Good.Equipment.HpType)
                : null;
            if (hp == null &&
                count > CargoUtilities.GetItemLimit(Player.Character.Items, Player.Character.Ship, g.Good.Equipment))
            {
                FLLog.Error("Player", $"{Player.Name} tried to overfill cargo hold");
                return Task.FromResult(false);
            }

            using (var c = Player.Character.BeginTransaction())
            {
                if (hp != null)
                    c.AddCargo(g.Good.Equipment, hp, 1);
                else
                    c.AddCargo(g.Good.Equipment, null, count);
                c.UpdateCredits(Player.Character.Credits - cost);
            }

            Player.UpdateCurrentInventory();

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> SellGood(int id, int count)
    {
        if (BaseData == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to sell good while in space");
            return Task.FromResult(false);
        }

        var slot = Player.Character.Items.FirstOrDefault(x => x.ID == id);
        if (slot == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to sell unknown slot {id}");
            return Task.FromResult(false);
        }

        if (slot.Count < count)
        {
            FLLog.Error("Player", $"{Player.Name} tried to oversell slot ({slot.Equipment?.Nickname ?? "null"})");
            return Task.FromResult(false);
        }

        ulong unitPrice = BaseData.GetUnitPrice(slot.Equipment);
        if (slot.Equipment is not CommodityEquipment)
            unitPrice = (ulong) (unitPrice * TradeConstants.EQUIP_RESALE_MULTIPLIER);
        using (var c = Player.Character.BeginTransaction())
        {
            c.RemoveCargo(slot, count);
            c.UpdateCredits(Player.Character.Credits + (long) ((ulong) count * unitPrice));
        }
        Player.UpdateCurrentInventory();
        return Task.FromResult(true);
    }

    public Task<ShipPackageInfo> GetShipPackage(int package)
    {
        var resolved = Player.Game.GameData.Items.GetShipPackage((uint) package);
        if (resolved == null)
        {
            return Task.FromResult<ShipPackageInfo>(null);
        }

        var spi = new ShipPackageInfo();
        spi.Included = resolved.Addons.Select(x =>
        {
            return new IncludedGood()
            {
                EquipCRC = x.Equipment.CRC,
                Hardpoint = string.IsNullOrWhiteSpace(x.Hardpoint) ? "internal" : x.Hardpoint,
                Amount = x.Amount
            };
        }).ToArray();
        return Task.FromResult(spi);
    }

    public Task<ShipPurchaseStatus> PurchaseShip(int package, MountId[] mountedPlayer, MountId[] mountedPackage,
        SellCount[] sellPlayer,
        SellCount[] sellPackage)
    {
        var resolved = Player.Game.GameData.Items.GetShipPackage((uint) package);
        if (resolved == null) return Task.FromResult(ShipPurchaseStatus.Fail);
        if (BaseData.SoldShips.All(x => x.Package != resolved))
        {
            FLLog.Error("Player", $"{Player.Name} tried to purchase ship package not available on base");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        var included = new List<PackageAddon>();
        foreach (var a in resolved.Addons)
            included.Add(new PackageAddon() {Equipment = a.Equipment, Amount = a.Amount});
        long shipPrice = resolved.BasePrice;
        //Sell included Items
        foreach (var item in sellPackage)
        {
            var a = included[item.ID];
            if (a == null) return Task.FromResult(ShipPurchaseStatus.Fail);
            if (item.Count > a.Amount) return Task.FromResult(ShipPurchaseStatus.Fail);
            var price = BaseData.GetUnitPrice(a.Equipment);
            shipPrice -= (long) price * item.Count;
            a.Amount -= item.Count;
            if (a.Amount <= 0)
                included[item.ID] = null;
        }

        if (shipPrice < 0) shipPrice = 0;
        //Deduct ship worth
        shipPrice -= (long) Player.GetShipWorth();
        //Add price of rest of items
        foreach (var a in included)
        {
            if (a == null) continue;
            var price = BaseData.GetUnitPrice(a.Equipment);
            shipPrice += (long) price * a.Amount;
        }

        Dictionary<int, int> counts = new Dictionary<int, int>();
        //Calculate player items price
        foreach (var item in sellPlayer)
        {
            var slot = Player.Character.Items.FirstOrDefault(x => x.ID == item.ID);
            if (slot == null)
            {
                FLLog.Error("Player", $"{Player.Name} tried to sell unknown slot {item.ID}");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            if (!counts.TryGetValue(slot.ID, out int count))
            {
                count = counts[slot.ID] = slot.Count;
            }

            if (count < item.Count)
            {
                FLLog.Error("Player",
                    $"{Player.Name} tried to oversell slot ({slot.Equipment.Nickname}, {count} < {item.Count})");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            var price = BaseData.GetUnitPrice(slot.Equipment);
            if (slot.Equipment is not CommodityEquipment)
                price = (ulong) (price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
            shipPrice -= (long) price * item.Count;
            counts[slot.ID] = (count - item.Count);
        }

        //Check if we have credits
        if (shipPrice > Player.Character.Credits)
        {
            FLLog.Error("Player", $"{Player.Name} does not have enough credits");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        //Check that all mounts are valid
        HashSet<int> mountedP = new HashSet<int>();
        HashSet<int> mountedInc = new HashSet<int>();
        HashSet<string> usedHardpoints = new HashSet<string>();
        foreach (var item in mountedPackage)
        {
            if (included[item.ID] == null)
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount sold item");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            var hp = item.Hardpoint.ToLowerInvariant();
            if (mountedInc.Contains(item.ID))
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount from package twice");
                mountedInc.Add(item.ID);
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            if (hp != "internal" && usedHardpoints.Contains(hp))
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount to hardpoint {hp} twice");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            if (hp != "internal")
            {
                usedHardpoints.Add(hp);
            }
        }

        foreach (var item in mountedPlayer)
        {
            var slot = Player.Character.Items.FirstOrDefault(x => x.ID == item.ID);
            if (slot == null)
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount non-existant item");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            if (counts.TryGetValue(item.ID, out var nc) && nc == 0)
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount sold item");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            var hp = item.Hardpoint.ToLowerInvariant();
            if (mountedP.Contains(item.ID))
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount item twice");
                mountedP.Add(item.ID);
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            if (hp != "internal" && usedHardpoints.Contains(hp))
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount to hardpoint {hp} twice");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            if (hp != "internal")
            {
                usedHardpoints.Add(hp);
            }
        }

        var newShip = Player.Game.GameData.Items.Ships.Get(resolved.Ship);
        float volume = 0;
        foreach (var item in Player.Character.Items)
        {
            counts.TryGetValue(item.ID, out var soldAmount);
            volume += item.Equipment.Volume * (item.Count - soldAmount);
        }

        foreach (var item in included)
        {
            if (item == null) continue;
            volume += item.Equipment.Volume * item.Amount;
        }

        if (volume > newShip.HoldSize)
        {
            FLLog.Error("Player", $"{Player.Name} tried to overfill new ship hold");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        using (var c = Player.Character.BeginTransaction())
        {
            //Remove sold items
            foreach (var item in counts)
            {
                var slot = Player.Character.Items.FirstOrDefault(x => x.ID == item.Key);
                c.RemoveCargo(slot, slot.Count - item.Value);
            }

            //Unmount items and remove items without a good
            List<NetCargo> toRemove = new List<NetCargo>();
            foreach (var item in Player.Character.Items)
            {
                item.Hardpoint = null;
                if (item.Equipment.Good == null)
                    toRemove.Add(item);
            }

            foreach (var item in toRemove)
                c.RemoveCargo(item, item.Count);
            //Set Ship
            c.UpdateShip(Player.Game.GameData.Items.Ships.Get(resolved.Ship));
            //Install new cargo and mount
            foreach (var item in mountedPlayer)
            {
                var slot = Player.Character.Items.FirstOrDefault(x => x.ID == item.ID);
                slot.Hardpoint = item.Hardpoint;
            }

            foreach (var item in mountedPackage)
            {
                var inc = included[item.ID];
                c.AddCargo(inc.Equipment, item.Hardpoint, inc.Amount);
                included[item.ID] = null;
            }

            foreach (var item in included)
            {
                if (item == null) continue;
                c.AddCargo(item.Equipment, item.Equipment.Good == null ? item.Hardpoint : null,
                    item.Amount);
            }

            c.UpdateCredits(Player.Character.Credits - shipPrice);
        }
        Player.UpdateCurrentInventory();
        //Success
        return Task.FromResult(shipPrice < 0 ? ShipPurchaseStatus.SuccessGainCredits : ShipPurchaseStatus.Success);
    }

    public Task<bool> Unmount(string hardpoint)
    {
        if (BaseData == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to unmount good while in space");
            return Task.FromResult(false);
        }

        var equip = Player.Character.Items.FirstOrDefault(x =>
            hardpoint.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase));
        if (equip == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to unmount empty hardpoint");
            return Task.FromResult(false);
        }

        equip.Hardpoint = null;
        Player.UpdateCurrentInventory();
        return Task.FromResult(true);
    }

    public Task<bool> Mount(int id)
    {
        if (BaseData == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to mount good while in space");
            return Task.FromResult(false);
        }

        var slot = Player.Character.Items.FirstOrDefault(x => x.ID == id);
        if (slot == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to mount unknown slot {id}");
            return Task.FromResult(false);
        }

        if (!string.IsNullOrEmpty(slot.Hardpoint))
        {
            FLLog.Error("Player", $"{Player.Name} tried to mount already mounted item {id}");
            return Task.FromResult(false);
        }

        string hp = FirstAvailableHardpoint(slot.Equipment.HpType);
        if (hp == null)
        {
            FLLog.Error("Player",
                $"{Player.Name} has no hp available to mount {slot.Equipment.Nickname} ({slot.Equipment.HpType})");
            return Task.FromResult(false);
        }

        using (var c = Player.Character.BeginTransaction())
        {
            slot.Hardpoint = hp;
            c.CargoModified();
        }

        Player.UpdateCurrentInventory();
        return Task.FromResult(true);
    }
}
