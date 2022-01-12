// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.Entities.Character;

namespace LibreLancer
{
    public class NetCharacter
    {
        public string Name;
        public string Base { get; private set; }
        public string System { get; private set; }
        public Vector3 Position { get; private set; }
        public long Credits { get; private set; }
        public GameData.Ship Ship;
        public List<NetCargo> Items;
        private long charId;
        GameDataManager gData;
        private DatabaseCharacter dbChar;
        private int _itemID;

        public void UpdatePosition(string _base, string sys, Vector3 pos)
        {
            Base = _base;
            System = sys;
            Position = pos;
            if (dbChar != null)
            {
                dbChar.Character.Base = _base;
                dbChar.Character.System = sys;
                dbChar.Character.X = pos.X;
                dbChar.Character.Y = pos.Y;
                dbChar.Character.Z = pos.Z;
                dbChar.ApplyChanges();
            }
        }
        
        public static NetCharacter FromDb(long id, GameServer game)
        {
            var db = game.Database.GetCharacter(id);
            var nc = new NetCharacter();
            nc.Name = db.Character.Name;
            nc.gData = game.GameData;
            nc.dbChar = db;
            nc.charId = id;
            nc.Base = db.Character.Base;
            nc.System = db.Character.System;
            nc.Position = new Vector3(db.Character.X, db.Character.Y, db.Character.Z);
            nc.Ship = game.GameData.GetShip(db.Character.Ship);
            nc.Credits = db.Character.Money;
            nc.Items = new List<NetCargo>();
            foreach (var cargo in db.Character.Items)
            {
                var resolved = game.GameData.GetEquipment(cargo.ItemName);
                if (resolved == null) continue;
                nc.Items.Add(new NetCargo()
                {
                    Count = (int)cargo.ItemCount, 
                    Hardpoint = cargo.Hardpoint,
                    Health = cargo.Health,
                    Equipment = resolved, 
                    DbItem = cargo
                });
            }
            return nc;
        }

        public void UpdateCredits(long credits)
        {
            Credits = credits;
            if (dbChar != null)
            {
                dbChar.Character.Money = credits;
                dbChar.ApplyChanges();
            }
        }

        public void ItemModified(NetCargo cargo)
        {
            if (dbChar != null) {
                dbChar.ApplyChanges();
            }
        }

        public void AddCargo(GameData.Items.Equipment equip, string hardpoint, int count)
        {
            if (equip.Good?.Ini.Combinable ?? false) 
            {
                if (!string.IsNullOrEmpty(hardpoint))
                {
                    throw new InvalidOperationException("Tried to mount combinable item");
                }
                var slot = Items.FirstOrDefault(x => equip.Good.Equipment == x.Equipment);
                if (slot == null)
                {
                    CargoItem dbItem = null;
                    if (dbChar != null) {
                        dbItem = new CargoItem() {ItemCount = count, ItemName = equip.Nickname};
                        dbChar.Character.Items.Add(dbItem);
                        dbChar.ApplyChanges();
                    }
                    Items.Add(new NetCargo() {Equipment = equip, Count = count, DbItem = dbItem});
                }
                else
                {
                    if (dbChar != null)
                    {
                        slot.DbItem.ItemCount += count;
                        dbChar.ApplyChanges();
                    }
                    slot.Count += count;
                }
            } else {
                CargoItem dbItem = null;
                if (dbChar != null)
                {
                    dbItem = new CargoItem() {ItemCount = count, Hardpoint = hardpoint, ItemName = equip.Nickname};
                    dbChar.Character.Items.Add(dbItem);
                    dbChar.ApplyChanges();
                }
                Items.Add(new NetCargo() { Equipment =  equip, Hardpoint = hardpoint, Count = count, DbItem = dbItem });
            }
        }

        public void SetShip(GameData.Ship ship)
        {
            Ship = ship;
            if (dbChar != null) {
                dbChar.Character.Ship = ship.Nickname;
                dbChar.ApplyChanges();
            }
        }
        
        public void RemoveCargo(NetCargo slot, int amount)
        {
            slot.Count -= amount;
            if (slot.Count <= 0)
            {
                Items.Remove(slot);
                if (slot.DbItem != null)
                {
                    dbChar.Character.Items.Remove(slot.DbItem);
                    dbChar.ApplyChanges();
                }
            } 
            else if(slot.DbItem != null)
            {
                slot.DbItem.ItemCount = slot.Count;
            }
        }

        public NetShipLoadout EncodeLoadout()
        {
            var sl = new NetShipLoadout();
            sl.ShipCRC = Ship.CRC;
            sl.Items = new List<NetShipCargo>(Items.Count);
            foreach (var c in Items)
            {
                sl.Items.Add(new NetShipCargo(
                    c.ID, c.Equipment.CRC,
                    CrcTool.HardpointCrc(c.Hardpoint), (byte) (c.Health * 255f), 
                    c.Count
                ));
            }
            return sl;
        }

        public SelectableCharacter ToSelectable()
        {
            var selectable = new SelectableCharacter();
            selectable.Id = charId;
            selectable.Rank = 1;
            selectable.Ship = Ship.Nickname;
            selectable.Name = Name;
            selectable.Funds = Credits;
            selectable.Location = gData.GetBase(Base).System;
            return selectable;
        }

        public void Dispose()
        {
            if (dbChar != null)
            {
                dbChar.Dispose();
                dbChar = null;
            }
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
        public CargoItem DbItem;
    }
}