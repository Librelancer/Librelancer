// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Data.Schema.Ships;
using LibreLancer.Database;
using LibreLancer.Entities.Character;
using LibreLancer.Entities.Enums;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using Ship = LibreLancer.Data.GameData.Ship;
using VisitEntry = LibreLancer.Data.Schema.Save.VisitEntry;

namespace LibreLancer.Server
{
    public class NetCharacter
    {
        public string Name;

        public bool Admin;
        public string Base { get; private set; }
        public string System { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Orientation { get; private set; } = Quaternion.Identity;
        public long Credits { get; private set; }

        public double Time { get; private set; }

        private NetPlayerStatistics statistics;
        public NetPlayerStatistics Statistics => statistics;

        public uint Rank { get; private set; }

        public ReputationCollection Reputation = new ReputationCollection();

        public Ship Ship { get; private set; }
        public List<NetCargo> Items = new List<NetCargo>();
        Dictionary<uint, VisitFlags> visited = new Dictionary<uint, VisitFlags>();
        private HashSet<uint> basesVisited = new();
        private HashSet<uint> systemsVisited = new();
        private HashSet<uint> holesVisited = new();

        private long charId;
        GameDataManager gData;
        private DatabaseCharacter dbChar;

        public long ID => charId;

        private int transactionCount;

        // Individual ship kill types are for Single player saves only.
        // In MP we simply store the statistics.
        Dictionary<uint, int> shipKillCounts = new Dictionary<uint, int>();
        public void IncrementShipKillCount(Ship ship)
        {
            shipKillCounts.TryGetValue(ship.CRC, out var count);
            shipKillCounts[ship.CRC] = count + 1;
        }

        public (uint Ship, int Count)[] GetShipKillCounts() =>
            shipKillCounts.Select(x => (x.Key, x.Value)).ToArray();

        public VisitEntry[] GetAllVisitFlags() =>
            visited.Select(x => new VisitEntry(x.Key, (int)x.Value)).ToArray();

        public VisitFlags GetVisitFlags(uint hash)
        {
            visited.TryGetValue(hash, out var flags);
            return flags;
        }

        public bool IsSystemVisited(uint hash) => systemsVisited.Contains(hash);
        public bool IsBaseVisited(uint hash) => basesVisited.Contains(hash);
        public bool IsJumpholeVisited(uint hash) => holesVisited.Contains(hash);

        public uint[] GetSystemsVisited() => systemsVisited.ToArray();
        public uint[] GetBasesVisited() => basesVisited.ToArray();
        public uint[] GetHolesVisited() => holesVisited.ToArray();

        public CharacterTransaction BeginTransaction()
        {
            if (Interlocked.Increment(ref transactionCount) > 1)
                throw new Exception("CharacterTransaction may only be created once");
            return new CharacterTransaction(this, null);
        }


        public class CharacterTransaction : IDisposable
        {
            private NetCharacter nc;
            private Character newEntity;
            private bool cargoDirty = false;
            private Dictionary<uint, Visit> updatedVisits = new();
            private Dictionary<Faction, float> updatedReputations = new();
            private List<VisitHistoryInput> visitHistory = new();

            internal CharacterTransaction(NetCharacter n, Character newEntity)
            {
                nc = n;
                this.newEntity = newEntity;
            }

            public void UpdatePosition(string _base, string sys, Vector3 pos, Quaternion orient)
            {
                nc.Base = _base;
                nc.System = sys;
                nc.Position = pos;
                nc.Orientation = orient;
            }


            public void UpdateReputation(Faction faction, float reputation)
            {
                updatedReputations[faction] = reputation;
                nc.Reputation.Reputations[faction] = reputation;
            }

            public void UpdateName(string name) => nc.Name = name;

            public void UpdateFightersKilled(long killed)
            {
                nc.statistics.FightersKilled = killed;
            }

            public void UpdateFreightersKilled(long killed)
            {
                nc.statistics.FreightersKilled = killed;
            }

            public void UpdateBattleshipsKilled(long killed)
            {
                nc.statistics.BattleshipsKilled = killed;
            }

            public void UpdateVisitFlags(uint hash, VisitFlags visit)
            {
                if (!nc.visited.TryGetValue(hash, out VisitFlags old) ||
                    old != visit)
                {
                    updatedVisits[hash] = (Visit)(uint)visit;
                    nc.visited[hash] = visit;
                }
            }

            public void VisitBase(uint hash)
            {
                if (nc.basesVisited.Add(hash))
                {
                    visitHistory.Add(new(VisitHistoryKind.Base, hash));
                    nc.statistics.BasesVisited++;
                }
            }

            public void VisitSystem(uint hash)
            {
                if (nc.systemsVisited.Add(hash))
                {
                    visitHistory.Add(new(VisitHistoryKind.System, hash));
                    nc.statistics.SystemsVisited++;
                }
            }

            public void VisitJumphole(uint hash)
            {
                if (nc.holesVisited.Add(hash))
                {
                    visitHistory.Add(new(VisitHistoryKind.Jumphole, hash));
                    nc.statistics.JumpHolesFound++;
                }
            }

            public void UpdateTransportsKilled(long killed)
            {
                nc.statistics.TransportsKilled = killed;
            }

            public void UpdateCredits(long credits)
            {
                nc.Credits = credits;
            }

            public void UpdateTime(double time)
            {
                nc.Time = time;
            }

            public void UpdateShip(Ship ship)
            {
                nc.Ship = ship;
            }

            private string _costume;
            private string _comCostume;
            public void UpdateCostume(string costume)
            {
                _costume = costume;
            }

            public void UpdateComCostume(string comCostume)
            {
                _comCostume = comCostume;
            }

            private List<long> cargoToDelete = new List<long>();

            public void CargoModified() => cargoDirty = true;

            public void AddCargo(Equipment equip, string hardpoint, int count)
            {
                cargoDirty = true;
                if (equip.Good?.Ini.Combinable ?? false)
                {
                    if (!string.IsNullOrEmpty(hardpoint))
                    {
                        throw new InvalidOperationException("Tried to mount combinable item");
                    }
                    var slot = nc.Items.FirstOrDefault(x => equip.Good.Equipment == x.Equipment);
                    if (slot == null)
                    {
                        CargoItem dbItem = null;
                        nc.Items.Add(new NetCargo() {Equipment = equip, Count = count });
                    }
                    else
                    {
                        slot.Count += count;
                    }
                } else {
                    nc.Items.Add(new NetCargo() { Equipment =  equip, Hardpoint = hardpoint, Count = count });
                }
            }

            public void ClearAllCargo()
            {
                foreach(var item in nc.Items.Where(x => x.DbItemId != 0))
                    cargoToDelete.Add(item.DbItemId);
                nc.Items = new List<NetCargo>();
            }

            public void RemoveCargo(NetCargo slot, int amount)
            {
                cargoDirty = true;
                slot.Count -= amount;
                if (slot.Count <= 0)
                {
                    nc.Items.Remove(slot);
                    cargoToDelete.Add(slot.DbItemId);
                }
            }

            public void UpdateRank(uint rank)
            {
                nc.Rank = rank;
            }

            void Update(Character c, List<(NetCargo cargo, CargoItem dbItem)> newItems)
            {
                c.Name = nc.Name;
                c.Base = nc.Base;
                c.System = nc.System;
                c.X = nc.Position.X;
                c.Y = nc.Position.Y;
                c.Z = nc.Position.Z;
                c.RotationX = nc.Orientation.X;
                c.RotationY = nc.Orientation.Y;
                c.RotationZ = nc.Orientation.Z;
                c.RotationW = nc.Orientation.W;
                c.Money = nc.Credits;
                c.Ship = nc.Ship?.Nickname;
                c.Costume = _costume;
                c.ComCostume = _comCostume;
                c.Rank = nc.Rank;
                c.Time = nc.Time;
                c.FightersKilled = nc.statistics.FightersKilled;
                c.TransportsKilled = nc.statistics.TransportsKilled;
                c.CapitalKills = nc.statistics.BattleshipsKilled;
                c.FreightersKilled = nc.statistics.FreightersKilled;
                if (cargoDirty)
                {
                    foreach (var item in cargoToDelete)
                    {
                        var dbItem = c.Items.FirstOrDefault(x => item == x.Id);
                        if (dbItem != null) c.Items.Remove(dbItem);
                    }
                    foreach (var item in nc.Items)
                    {
                        var dbItem = item.DbItemId == 0 ? null :
                            c.Items.FirstOrDefault(x => item.DbItemId == x.Id);
                        //Add new items
                        if (dbItem == null) {
                            dbItem = new CargoItem();
                            newItems.Add((item, dbItem));
                            c.Items.Add(dbItem);
                        }
                        //Update existing
                        dbItem.ItemName = item.Equipment?.Nickname;
                        dbItem.ItemCount = item.Count;
                        dbItem.Hardpoint = item.Hardpoint;
                        dbItem.Health = item.Health;
                    }
                }
            }

            public void Dispose()
            {
                if (Interlocked.Decrement(ref nc.transactionCount) < 0)
                    throw new Exception("Transaction already committed");
                var newItems = new List<(NetCargo cargo, CargoItem dbItem)>();
                if (newEntity != null)
                {
                    Update(newEntity, newItems);
                    foreach (var visit in nc.visited)
                    {
                        newEntity.VisitEntries.Add(new()
                        {
                            Hash = visit.Key,
                            VisitValue = (Visit)(uint)visit.Value
                        });
                    }

                    foreach (var history in visitHistory)
                    {
                        newEntity.VisitHistoryEntries.Add(new()
                        {
                            Kind = history.Kind,
                            Hash = history.Hash,
                        });
                    }
                    foreach (var rep in updatedReputations)
                    {
                        newEntity.Reputations.Add(new Reputation() { RepGroup = rep.Key.Nickname, ReputationValue = rep.Value });
                    }
                }
                else
                {
                    //dbChar updates are guaranteed to execute in order
                    //so this is safe to do asynchronously
                    nc.dbChar?.Update(c => Update(c, newItems), cargoDirty)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .OnCompleted(() =>
                    {
                        foreach (var i in newItems)
                        {
                            i.cargo.DbItemId = i.dbItem.Id;
                        }
                    });
                    if (updatedVisits.Count > 0)
                    {
                        nc.dbChar?.UpdateVisitFlags(updatedVisits);
                    }
                    if (visitHistory.Count > 0)
                    {
                        nc.dbChar?.AddVisitHistory(visitHistory);
                    }
                    if (updatedReputations.Count > 0)
                    {
                        nc.dbChar?.UpdateFactionReps(
                            updatedReputations.Select(x =>
                                new KeyValuePair<string, float>(x.Key.Nickname, x.Value)).ToArray());
                    }
                }
            }
        }

        static IEnumerable<(Faction fac, float rep)> RepFromSave(GameServer game, SaveGame sg)
        {
            foreach (var h in sg.Player.House)
            {
                var f = game.GameData.Items.Factions.Get(h.Group);
                if (f != null)
                    yield return (f, h.Reputation);
            }
        }

        static NetCharacter FromSaveGameInternal(GameServer game, SaveGame sg, Character db = null)
        {
            var nc = new NetCharacter();
            nc.gData = game.GameData;
            nc.Admin = db == null;
            nc.transactionCount++;
            var stats = new NetPlayerStatistics();
            using (var c = new CharacterTransaction(nc, db))
            {
                c.UpdateName(sg.Player.Name);
                c.UpdateCredits(sg.Player.Money);
                c.UpdateShip(game.GameData.Items.Ships.Get(sg.Player.ShipArchetype));
                c.UpdateCostume(sg.Player.Costume);
                c.UpdateComCostume(sg.Player.ComCostume);
                c.UpdatePosition(sg.Player.Base, sg.Player.System, sg.Player.Position, Quaternion.Identity);
                c.UpdateRank((uint)sg.Player.Rank);
                c.UpdateTime(sg.Time?.Seconds ?? 0);
                foreach (var eq in sg.Player.Equip)
                {
                    var hp = eq.Hardpoint;
                    if (string.IsNullOrEmpty(hp)) hp = "internal";
                    var equip = game.GameData.Items.Equipment.Get(eq.Item);
                    if (equip != null)
                        c.AddCargo(equip, hp, 1);
                }
                foreach (var cg in sg.Player.Cargo)
                {
                    var equip = game.GameData.Items.Equipment.Get(cg.Item);
                    if (equip != null)
                        c.AddCargo(equip, null, cg.Count);
                }
                foreach (var rep in RepFromSave(game, sg))
                {
                    c.UpdateReputation(rep.fac, rep.rep);
                }
                foreach (var visit in sg.Player.Visit)
                {
                    c.UpdateVisitFlags(visit.Obj.Hash, (VisitFlags)visit.Visit);
                }
                foreach (var v in (sg.MPlayer?.BaseVisited) ?? [])
                {
                    c.VisitBase((uint)v);
                }
                foreach (var v in (sg.MPlayer?.SysVisited) ?? [])
                {
                    c.VisitSystem((uint)v);
                }
                foreach (var v in (sg.MPlayer?.HolesVisited) ?? [])
                {
                    c.VisitJumphole((uint)v);
                }
                foreach (var ks in (sg.MPlayer?.ShipTypeKilled) ?? [])
                {
                    var ship = game.GameData.Items.Ships.Get(ks.Item);
                    if (ship == null)
                    {
                        continue;
                    }
                    switch (ship.ShipType)
                    {
                        case ShipType.Fighter:
                            stats.FightersKilled += ks.Count;
                            break;
                        case ShipType.Freighter:
                            stats.FreightersKilled += ks.Count;
                            break;
                        case ShipType.Capital:
                            stats.BattleshipsKilled += ks.Count;
                            break;
                        case ShipType.Transport:
                            stats.TransportsKilled += ks.Count;
                            break;
                    }
                    nc.shipKillCounts.TryGetValue(ship.CRC, out var count);
                    nc.shipKillCounts[ship.CRC] = count + ks.Count;
                }
                c.UpdateFightersKilled(stats.FightersKilled);
                c.UpdateFreightersKilled(stats.FreightersKilled);
                c.UpdateTransportsKilled(stats.TransportsKilled);
                c.UpdateBattleshipsKilled(stats.BattleshipsKilled);
            }
            return nc;
        }


        public static void SaveToDbCharacter(GameServer game, SaveGame sg, Character db) =>
            FromSaveGameInternal(game, sg, db);

        public static NetCharacter OpenSaveGame(GameServer game, SaveGame sg) =>
            FromSaveGameInternal(game, sg);

        public static async Task<NetCharacter> FromDb(long id, GameServer game)
        {
            var db = await game.Database.GetCharacter(id);
            var nc = new NetCharacter();
            var c = await db.GetCharacter();
            nc.Admin = c.IsAdmin;
            nc.Reputation = new ReputationCollection();
            foreach (var rep in c.Reputations)
            {
                var f = game.GameData.Items.Factions.Get(rep.RepGroup);
                if (f != null) nc.Reputation.Reputations[f] = rep.ReputationValue;
            }
            nc.Name = c.Name;
            nc.Time = c.Time;
            nc.gData = game.GameData;
            nc.dbChar = db;
            nc.charId = id;
            nc.Base = c.Base;
            nc.System = c.System;
            nc.Position = new Vector3(c.X, c.Y, c.Z);
            nc.Orientation = new Quaternion(c.RotationX, c.RotationY, c.RotationZ, c.RotationW);
            nc.Ship = game.GameData.Items.Ships.Get(c.Ship);
            nc.Credits = c.Money;
            nc.Items = new List<NetCargo>();
            nc.statistics = new()
            {
                FightersKilled = c.FightersKilled,
                FreightersKilled = c.FreightersKilled,
                BattleshipsKilled = c.CapitalKills,
                TransportsKilled = c.TransportsKilled
            };
            foreach (var cargo in c.Items)
            {
                var resolved = game.GameData.Items.Equipment.Get(cargo.ItemName);
                if (resolved == null) continue;
                nc.Items.Add(new NetCargo()
                {
                    Count = (int)cargo.ItemCount,
                    Hardpoint = cargo.Hardpoint,
                    Health = cargo.Health,
                    Equipment = resolved,
                    DbItemId = cargo.Id
                });
            }
            foreach (var visit in (c.VisitEntries ?? []))
            {
                nc.visited[visit.Hash] = (VisitFlags)(uint)visit.VisitValue;
            }

            foreach (var h in (c.VisitHistoryEntries ?? []))
            {
                switch (h.Kind)
                {
                    case VisitHistoryKind.Base:
                        if (nc.basesVisited.Add(h.Hash))
                        {
                            nc.statistics.BasesVisited++;
                        }
                        break;
                    case VisitHistoryKind.Jumphole:
                        if (nc.holesVisited.Add(h.Hash))
                        {
                            nc.statistics.JumpHolesFound++;
                        }
                        break;
                    case VisitHistoryKind.System:
                        if (nc.systemsVisited.Add(h.Hash))
                        {
                            nc.statistics.SystemsVisited++;
                        }
                        break;
                }
            }
            return nc;
        }

        public NetLoadout EncodeLoadout()
        {
            var sl = new NetLoadout();
            sl.ArchetypeCrc = Ship?.CRC ?? 0;
            sl.Items = new List<NetShipCargo>(Items.Count);
            foreach (var c in Items)
            {
                sl.Items.Add(new NetShipCargo(
                    c.ID, c.Equipment.CRC,
                    c.Hardpoint, (byte) (c.Health * 255f),
                    c.Count
                ));
            }
            return sl;
        }

        public SelectableCharacter ToSelectable()
        {
            var selectable = new SelectableCharacter();
            selectable.Id = charId;
            selectable.Rank = (int)Rank;
            selectable.Ship = Ship.Nickname;
            selectable.Name = Name;
            selectable.Funds = Credits;
            selectable.Location = gData.Items.Bases.Get(Base).System;
            return selectable;
        }
    }

    public class NetCargo
    {
        private static int _id;
        public readonly int ID;
        public NetCargo()
        {
            ID = Interlocked.Increment(ref _id);
        }
        public NetCargo(int id)
        {
            ID = id;
        }
        public Equipment Equipment;
        public string Hardpoint;
        public float Health;
        public int Count;
        public long DbItemId;
    }
}
