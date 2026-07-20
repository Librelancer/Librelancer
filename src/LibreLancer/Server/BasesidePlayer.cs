using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.Market;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Entities.Character;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.RandomMissions;
using LibreLancer.World;

namespace LibreLancer.Server;

public class BasesidePlayer : IBasesidePlayer
{
    public Player Player;
    public Base? BaseData;
    private readonly List<GeneratedRandomMission> generatedMissions = [];
    public NetMissionOffer[] NetMissionOffers = [];
    private bool generatedMissionOffers;

    public BasesidePlayer(Player player, Base baseData)
    {
        BaseData = baseData;
        Player = player;
        GenerateMissionOffers("bar");
    }

    public void GenerateMissionOffers(string? roomNickname)
    {
        if (BaseData == null || generatedMissionOffers)
            return;

        generatedMissions.Clear();
        generatedMissionOffers = true;
        var offers = Player.Game.GameData.Items.GetRandomMissionOffers(BaseData, roomNickname: roomNickname);
        var systemIdsName = Player.Game.GameData.Items.Systems.Get(BaseData.System)?.IdsName ?? 0;
        var generatedOffers = new List<(RandomMissionOffer Offer, GeneratedRandomMission Mission)>();
        var random = Random.Shared;
        foreach (var offer in offers)
        {
            if (!RandomMissionGenerator.TryGenerate(Player.Game.GameData, offer, Player.Story?.MissionNum, out var generated))
                continue;
            generatedOffers.Add((offer, generated));
        }

        var selectedOffers = SelectMissionOffers(
            generatedOffers,
            GetMissionOfferCount(BaseData, generatedOffers.Count, random),
            random);
        generatedMissions.AddRange(selectedOffers);
        NetMissionOffers = selectedOffers
            .Select(x => CreateNetMissionOffer(x, systemIdsName))
            .ToArray();
    }

    static int GetMissionOfferCount(Base baseData, int availableCount, Random random)
    {
        if (availableCount <= 0)
            return 0;

        var min = Math.Clamp(baseData.MinMissionOffers, 0, availableCount);
        var max = baseData.MaxMissionOffers > 0
            ? Math.Clamp(baseData.MaxMissionOffers, min, availableCount)
            : availableCount;
        if (min == 0 && max > 0)
            min = 1;
        return min == max ? min : random.Next(min, max + 1);
    }

    static List<GeneratedRandomMission> SelectMissionOffers(
        List<(RandomMissionOffer Offer, GeneratedRandomMission Mission)> candidates,
        int count,
        Random random)
    {
        var selected = new List<GeneratedRandomMission>();
        var deferred = new List<GeneratedRandomMission>();
        var available = new List<(RandomMissionOffer Offer, GeneratedRandomMission Mission)>(candidates);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (selected.Count < count && available.Count > 0)
        {
            var index = WeightedPick(available, random);
            var candidate = available[index];
            available.RemoveAt(index);

            var key = string.Join("|",
                candidate.Offer.Faction?.Nickname,
                candidate.Mission.MissionType,
                candidate.Mission.Parameters.HostileFaction.Nickname,
                MissionLocationKey(candidate.Mission));
            if (seen.Add(key))
            {
                selected.Add(candidate.Mission);
            }
            else
            {
                deferred.Add(candidate.Mission);
            }
        }

        for (int i = 0; selected.Count < count && i < deferred.Count; i++)
            selected.Add(deferred[i]);
        return selected;
    }

    static int WeightedPick(List<(RandomMissionOffer Offer, GeneratedRandomMission Mission)> candidates, Random random)
    {
        var total = candidates.Sum(OfferWeight);
        var choice = random.NextSingle() * total;
        var cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += OfferWeight(candidates[i]);
            if (choice <= cumulative)
                return i;
        }
        return candidates.Count - 1;
    }

    static float OfferWeight((RandomMissionOffer Offer, GeneratedRandomMission Mission) item) =>
        Math.Max(0.0001f, item.Offer.Weight);

    static string MissionLocationKey(GeneratedRandomMission mission)
    {
        return mission.TargetLocation switch
        {
            Zone z => $"zone:{z.Nickname}:{z.IdsName}",
            Base b => $"base:{b.Nickname}:{b.IdsName}",
            SystemObject o => $"object:{o.Nickname}:{o.IdsName}",
            NamedItem n => $"item:{n.Nickname}:{n.IdsName}",
            IdsArgument i => $"ids:{i.Category}:{i.Ids}",
            StringArgument s => $"string:{s.Category}:{s.Value}",
            _ => $"target-zone:{mission.Parameters.TargetZone.Nickname}"
        };
    }

    public void AcceptMissionOffer(int id)
    {
        var idx = generatedMissions.FindIndex(x => x.Id == id);
        if (idx < 0)
        {
            FLLog.Warning("RandomMissions",
                $"{Player.Name} tried to accept unknown random mission seed {id}");
            return;
        }
        var mission = generatedMissions[idx];
        Player.StartRandomMission(mission, CreateNetMissionOffer(mission));
        ClearMissionOffers();
    }

    NetMissionOffer CreateNetMissionOffer(GeneratedRandomMission mission, int? systemIdsName = null)
    {
        var idsName = systemIdsName ??
            mission.Parameters.DestinationSystem?.IdsName ?? 0;
        return new NetMissionOffer
        {
            Id = mission.Id,
            //NpcIdsName = mission.Offer.Npc.IndividualName, 0
            FactionIdsName = mission.Parameters.OfferFaction?.IdsName ?? 0,
            SystemIdsName = idsName,
            Reward = mission.Parameters.Reward,
            MissionType = mission.MissionType,
            OfferText = mission.OfferText,
            TargetName = mission.TargetName
        };
    }

    public void ClearMissionOffers()
    {
        generatedMissions.Clear();
        generatedMissionOffers = false;
        NetMissionOffers = [];
    }

    private string? FirstAvailableHardpoint(string? hptype)
    {
        return CargoUtilities.CompatibleHardpoints(Player.Character!.Ship!, Player.Game.GameData.Items.Ini.HpTypes, hptype)
            .Where(possible => !Player.Character.Items.Any(x =>
                possible.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(possible => Player.Character.Ship.HardpointTypes[possible]
                .OrderBy(x => x.SortIndex).FirstOrDefault().SortIndex)
            .FirstOrDefault();
    }

    private bool IsValidMount(Ship ship, Equipment equipment, string? hardpoint)
    {
        if (string.IsNullOrWhiteSpace(hardpoint))
        {
            FLLog.Error("Player", $"{Player.Name} tried to mount {equipment.Nickname} to null hardpoint");
            return false;
        }

        if (equipment is ShieldEquipment or GunEquipment)
        {
            if (CargoUtilities.SupportsHardpoint(ship, Player.Game.GameData.Items.Ini.HpTypes, equipment.HpType, hardpoint))
            {
                // Only check shield and gun/turret mounts for now.
                // Other mounts like lights, engines, powercore etc. are mounted to
                // hardpoints without types.
                return true;
            }
            FLLog.Error("Player", $"{Player.Name} tried to mount {equipment.Nickname} to incompatible hardpoint {hardpoint}");
        }
        else
        {
            return true;
        }
        return false;
    }

    public Task<bool> PurchaseGood(string item, int count)
    {
        if (BaseData == null)
        {
            return Task.FromResult(false);
        }

        var g = BaseData.SoldGoods.FirstOrDefault(x =>
            x.Good.Equipment.Nickname.Equals(item, StringComparison.OrdinalIgnoreCase));

        if (g.Good == null)
        {
            return Task.FromResult(false);
        }

        var cost = (long) (g.Price * (ulong) count);

        if (Player.Character!.Credits < cost)
        {
            return Task.FromResult(false);
        }

        var hp = count == 1
            ? FirstAvailableHardpoint(g.Good.Equipment.HpType)
            : null;

        if (hp == null &&
            count > CargoUtilities.GetItemLimit(Player.Character.Items, Player.Character.Ship!, g.Good.Equipment))
        {
            FLLog.Error("Player", $"{Player.Name} tried to overfill cargo hold");
            return Task.FromResult(false);
        }

        using (var c = Player.Character.BeginTransaction())
        {
            if (hp != null)
            {
                c.AddCargo(g.Good.Equipment, hp, 1);
            }
            else
            {
                c.AddCargo(g.Good.Equipment, null, count);
            }

            c.UpdateCredits(Player.Character.Credits - cost);
        }

        Player.UpdateCurrentInventory();

        return Task.FromResult(true);

    }

    public Task<bool> SellGood(int id, int count)
    {
        if (BaseData == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to sell good while in space");
            return Task.FromResult(false);
        }

        var slot = Player.Character!.Items.FirstOrDefault(x => x.ID == id);

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

        var unitPrice = BaseData.GetUnitPrice(slot.Equipment!);

        if (slot.Equipment is not CommodityEquipment)
        {
            unitPrice = (ulong) (unitPrice * TradeConstants.EQUIP_RESALE_MULTIPLIER);
        }

        using (var c = Player.Character.BeginTransaction())
        {
            c.RemoveCargo(slot, count);
            c.UpdateCredits(Player.Character.Credits + (long) ((ulong) count * unitPrice));
        }

        Player.UpdateCurrentInventory();
        return Task.FromResult(true);
    }

    public Task<ShipPackageInfo?> GetShipPackage(int package)
    {
        var resolved = Player.Game.GameData.Items.GetShipPackage((uint) package);

        if (resolved == null)
        {
            return Task.FromResult<ShipPackageInfo?>(null);
        }

        var spi = new ShipPackageInfo
        {
            Included = resolved.Addons.Select(x => new IncludedGood()
            {
                EquipCRC = x.Equipment.CRC,
                Hardpoint = x.Hardpoint,
                Amount = x.Amount
            }).ToArray()
        };

        return Task.FromResult<ShipPackageInfo?>(spi);
    }

    // Mutable version
    class SaleAddon(PackageAddon addon)
    {
        public Equipment Equipment = addon.Equipment;
        public string? Hardpoint = addon.Hardpoint;
        public int Amount = addon.Amount;
    }


    public Task<ShipPurchaseStatus> PurchaseShip(int package, MountId[] mountedPlayer, MountId[] mountedPackage,
        SellCount[] sellPlayer,
        SellCount[] sellPackage)
    {
        var resolved = Player.Game.GameData.Items.GetShipPackage((uint) package);

        if (resolved == null)
        {
            FLLog.Error("Player", $"Couldn't find ship package {package}");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        if (BaseData == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to purchase ship while not on a base");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        var soldShip = BaseData.SoldShips.FirstOrDefault(x => x.Package == resolved);

        if (soldShip == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to purchase ship package not available on base");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        if ((int) Player.Character!.Rank < soldShip.Rank)
        {
            FLLog.Error("Player", $"{Player.Name} does not meet the rank requirement for ship package {resolved.Nickname}");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        var packagePrice = GetPackagePrice(resolved);

        if (Player.Character.Credits + (long) Player.GetShipWorth() + GetPlayerCargoWorth() < packagePrice)
        {
            FLLog.Error("Player", $"{Player.Name} does not have enough total value for ship package {resolved.Nickname}");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        var included = resolved.Addons.Select(x => new SaleAddon(x)).ToList<SaleAddon?>();

        var shipPrice = packagePrice - (long) Player.GetShipWorth();

        // Sell included Items
        foreach (var item in sellPackage)
        {
            var a = included[item.ID];
            if (item.Count > a.Amount)
            {
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            var price = BaseData!.GetUnitPrice(a.Equipment);
            shipPrice -= (long) price * item.Count;
            a.Amount -= item.Count;

            if (a.Amount <= 0)
            {
                included[item.ID] = null;
            }
        }

        Dictionary<int, int> counts = new Dictionary<int, int>();

        // Calculate player items price
        foreach (var item in sellPlayer)
        {
            var slot = Player.Character!.Items.FirstOrDefault(x => x.ID == item.ID);

            if (slot == null)
            {
                FLLog.Error("Player", $"{Player.Name} tried to sell unknown slot {item.ID}");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            if (!counts.TryGetValue(slot.ID, out var count))
            {
                count = counts[slot.ID] = slot.Count;
            }

            if (count < item.Count)
            {
                FLLog.Error("Player",
                    $"{Player.Name} tried to oversell slot ({slot.Equipment!.Nickname}, {count} < {item.Count})");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            var price = BaseData!.GetUnitPrice(slot.Equipment!);

            if (slot.Equipment is not CommodityEquipment)
            {
                price = (ulong) (price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
            }

            shipPrice -= (long) price * item.Count;
            counts[slot.ID] = (count - item.Count);
        }

        // Check if we have credits
        if (shipPrice > Player.Character!.Credits)
        {
            FLLog.Error("Player", $"{Player.Name} does not have enough credits");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        // Check that all mounts are valid
        HashSet<int> mountedP = [];
        HashSet<int> mountedInc = [];
        HashSet<string> usedHardpoints = [];

        foreach (var item in mountedPackage)
        {
            if (included[item.ID] == null)
            {
                FLLog.Error("Player", $"{Player.Name} tried to mount sold item");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

            var hp = item.Hardpoint!.ToLowerInvariant();
            var addon = included[item.ID]!;

            if (!IsValidMount(resolved.Ship, addon.Equipment, item.Hardpoint))
            {
                FLLog.Error("Player", "IsValidMount call failed");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

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

            var hp = item.Hardpoint!.ToLowerInvariant();

            if (!IsValidMount(resolved.Ship, slot.Equipment!, item.Hardpoint))
            {
                FLLog.Error("Player", "IsValidMount call failed");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }

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

        float volume = 0;

        foreach (var item in Player.Character.Items)
        {
            counts.TryGetValue(item.ID, out var soldAmount);
            volume += item.Equipment!.Volume * (item.Count - soldAmount);
        }

        volume += included.OfType<PackageAddon>().Sum(item => item.Equipment.Volume * item.Amount);

        if (volume > resolved.Ship.HoldSize)
        {
            FLLog.Error("Player", $"{Player.Name} tried to overfill new ship hold");
            return Task.FromResult(ShipPurchaseStatus.Fail);
        }

        using (var c = Player.Character.BeginTransaction())
        {
            // Remove sold items
            foreach (var item in counts)
            {
                var slot = Player.Character.Items.First(x => x.ID == item.Key);
                c.RemoveCargo(slot, slot.Count - item.Value);
            }

            // Unmount items and remove items without a good
            List<NetCargo> toRemove = [];

            foreach (var item in Player.Character.Items)
            {
                item.Hardpoint = null;

                if (item.Equipment!.Good == null)
                {
                    toRemove.Add(item);
                }
            }

            foreach (var item in toRemove)
                c.RemoveCargo(item, item.Count);
            // Set Ship
            c.UpdateShip(resolved.Ship);

            // Install new cargo and mount
            foreach (var item in mountedPlayer)
            {
                var slot = Player.Character.Items.First(x => x.ID == item.ID);
                slot.Hardpoint = item.Hardpoint;
            }

            foreach (var item in mountedPackage)
            {
                var inc = included[item.ID];

                if (inc is null)
                {
                    continue;
                }

                c.AddCargo(inc.Equipment, item.Hardpoint, inc.Amount);
                included[item.ID] = null;
            }

            foreach (var item in included.OfType<SaleAddon>())
            {
                c.AddCargo(item.Equipment, item.Equipment.Good == null ? item.Hardpoint : null, item.Amount);
            }

            c.UpdateCredits(Player.Character.Credits - shipPrice);
        }

        Player.UpdateCurrentInventory();
        // Success
        return Task.FromResult(shipPrice < 0 ? ShipPurchaseStatus.SuccessGainCredits : ShipPurchaseStatus.Success);
    }

    private long GetPackagePrice(ShipPackage package)
    {
        var price = package.BasePrice;

        foreach (var addon in package.Addons)
        {
            price += (long) BaseData!.GetUnitPrice(addon.Equipment) * addon.Amount;
        }

        return price;
    }

    private long GetPlayerCargoWorth()
    {
        long worth = 0;

        foreach (var item in Player.Character!.Items)
        {
            if (item.Equipment?.Good == null)
            {
                continue;
            }

            var unitPrice = BaseData!.GetUnitPrice(item.Equipment);

            if (item.Equipment is not CommodityEquipment)
            {
                unitPrice = (ulong) (unitPrice * TradeConstants.EQUIP_RESALE_MULTIPLIER);
            }

            worth += (long) unitPrice * item.Count;
        }

        return worth;
    }

    public Task<bool> Unmount(string hardpoint)
    {
        if (BaseData == null)
        {
            FLLog.Error("Player", $"{Player.Name} tried to unmount good while in space");
            return Task.FromResult(false);
        }

        var equip = Player.Character!.Items.FirstOrDefault(x =>
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

        var slot = Player.Character!.Items.FirstOrDefault(x => x.ID == id);

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

        var hp = FirstAvailableHardpoint(slot.Equipment!.HpType);

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
