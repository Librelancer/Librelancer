// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.Data.Save;
using LibreLancer.Entities.Character;
using LibreLancer.GameData;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

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

        public uint Rank { get; private set; }

        public ReputationCollection Reputation = new ReputationCollection();

        public Ship Ship { get; private set; }
        public List<NetCargo> Items = new List<NetCargo>();

        private long charId;
        GameDataManager gData;
        private DatabaseCharacter dbChar;

        public long ID => charId;

        private int transactionCount;

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

            private Dictionary<Faction, float> updatedReputations = new Dictionary<Faction, float>();

            public void UpdateReputation(Faction faction, float reputation)
            {
                updatedReputations[faction] = reputation;
                nc.Reputation.Reputations[faction] = reputation;
            }

            public void UpdateName(string name) => nc.Name = name;

            public void UpdateCredits(long credits)
            {
                nc.Credits = credits;
            }

            public void UpdateShip(GameData.Ship ship)
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

            public void AddCargo(GameData.Items.Equipment equip, string hardpoint, int count)
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
                if (cargoDirty)
                {
                    foreach (var item in cargoToDelete)
                    {
                        var dbItem = c.Items.FirstOrDefault(x => item == x.Id);
                        if (dbItem != null) c.Items.Remove(dbItem);
                    }
                    foreach (var item in nc.Items) {
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

                foreach (var rep in updatedReputations)
                {
                    var ent = c.Reputations.FirstOrDefault(x =>
                        x.RepGroup.Equals(rep.Key.Nickname, StringComparison.OrdinalIgnoreCase));
                    if (ent == null)
                        c.Reputations.Add(new Reputation() { RepGroup = rep.Key.Nickname, ReputationValue = rep.Value });
                    else
                        ent.ReputationValue = rep.Value;
                }
            }

            public void Dispose()
            {
                if (Interlocked.Decrement(ref nc.transactionCount) < 0)
                    throw new Exception("Transaction already committed");
                var newItems = new List<(NetCargo cargo, CargoItem dbItem)>();
                if(newEntity != null)
                    Update(newEntity, newItems);
                else
                    nc.dbChar?.Update(c => Update(c, newItems));

                //Set the autogenerated ids up
                foreach (var i in newItems) {
                    i.cargo.DbItemId = i.dbItem.Id;
                }
            }
        }

        static IEnumerable<(GameData.Faction fac, float rep)> RepFromSave(GameServer game, SaveGame sg)
        {
            foreach (var h in sg.Player.House)
            {
                var f = game.GameData.Factions.Get(h.Group);
                if (f != null)
                    yield return (f, h.Reputation);
            }
        }

        public static NetCharacter FromSaveGame(GameServer game, SaveGame sg, Character db = null)
        {
            var nc = new NetCharacter();
            nc.gData = game.GameData;
            nc.Admin = db == null;
            nc.transactionCount++;
            using (var c = new CharacterTransaction(nc, db))
            {
                c.UpdateName(sg.Player.Name);
                c.UpdateCredits(sg.Player.Money);
                c.UpdateShip(game.GameData.Ships.Get(sg.Player.ShipArchetype));
                c.UpdateCostume(sg.Player.Costume);
                c.UpdateComCostume(sg.Player.ComCostume);
                c.UpdatePosition(sg.Player.Base, sg.Player.System, sg.Player.Position, Quaternion.Identity);
                c.UpdateRank((uint)sg.Player.Rank);
                foreach (var eq in sg.Player.Equip)
                {
                    var hp = eq.Hardpoint;
                    if (string.IsNullOrEmpty(hp)) hp = "internal";
                    var equip = game.GameData.Equipment.Get(eq.Item);
                    if (equip != null)
                        c.AddCargo(equip, hp, 1);
                }
                foreach (var cg in sg.Player.Cargo)
                {
                    var equip = game.GameData.Equipment.Get(cg.Item);
                    if (equip != null)
                        c.AddCargo(equip, null, cg.Count);
                }
                foreach (var rep in RepFromSave(game, sg))
                {
                    c.UpdateReputation(rep.fac, rep.rep);
                }
            }
            return nc;
        }

        public static NetCharacter FromDb(long id, GameServer game)
        {
            var db = game.Database.GetCharacter(id);
            var nc = new NetCharacter();
            var c = db.GetCharacter();
            nc.Admin = c.IsAdmin;
            nc.Reputation = new ReputationCollection();
            foreach (var rep in c.Reputations)
            {
                var f = game.GameData.Factions.Get(rep.RepGroup);
                if (f != null) nc.Reputation.Reputations[f] = rep.ReputationValue;
            }
            nc.Name = c.Name;
            nc.gData = game.GameData;
            nc.dbChar = db;
            nc.charId = id;
            nc.Base = c.Base;
            nc.System = c.System;
            nc.Position = new Vector3(c.X, c.Y, c.Z);
            nc.Orientation = new Quaternion(c.RotationX, c.RotationY, c.RotationZ, c.RotationW);
            nc.Ship = game.GameData.Ships.Get(c.Ship);
            nc.Credits = c.Money;
            nc.Items = new List<NetCargo>();
            foreach (var cargo in c.Items)
            {
                var resolved = game.GameData.Equipment.Get(cargo.ItemName);
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
            return nc;
        }

        public NetShipLoadout EncodeLoadout()
        {
            var sl = new NetShipLoadout();
            sl.ShipCRC = Ship?.CRC ?? 0;
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
            selectable.Location = gData.Bases.Get(Base).System;
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
        public GameData.Items.Equipment Equipment;
        public string Hardpoint;
        public float Health;
        public int Count;
        public long DbItemId;
    }
}
