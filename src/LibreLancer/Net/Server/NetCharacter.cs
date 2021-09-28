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
        public List<NetEquipment> Equipment;
        public List<NetCargo> Cargo;
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
            nc.Equipment = new List<NetEquipment>(db.Character.Equipment.Count);
            nc.Cargo = new List<NetCargo>();
            foreach(var equip in db.Character.Equipment)
            {
                var resolved = game.GameData.GetEquipment(equip.EquipmentNickname);
                if (resolved == null) continue;
                nc.Equipment.Add(new NetEquipment()
                {
                    Hardpoint = equip.EquipmentHardpoint,
                    Equipment = resolved,
                    Health = 1f,
                    DbItem = equip
                });
            }
            foreach (var cargo in db.Character.Cargo)
            {
                var resolved = game.GameData.GetEquipment(cargo.ItemName);
                if (resolved == null) continue;
                nc.Cargo.Add(new NetCargo() { Count = (int)cargo.ItemCount, Equipment = resolved, DbItem = cargo});
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

        public void AddCargo(GameData.Items.Equipment equip, int count)
        {
            if (equip.Good.Ini.Combinable)
            {
                var slot = Cargo.FirstOrDefault(x => equip.Good.Equipment == x.Equipment);
                if (slot == null)
                {
                    CargoItem dbItem = null;
                    if (dbChar != null)
                    {
                        dbItem = new CargoItem() {ItemCount = count, ItemName = equip.Nickname};
                        dbChar.Character.Cargo.Add(dbItem);
                        dbChar.ApplyChanges();
                    }

                    Cargo.Add(new NetCargo() {Equipment = equip, Count = count, DbItem = dbItem});
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
                    dbItem = new CargoItem() {ItemCount = count, ItemName = equip.Nickname};
                    dbChar.Character.Cargo.Add(dbItem);
                    dbChar.ApplyChanges();
                }
                Cargo.Add(new NetCargo() { Equipment =  equip, Count = count, DbItem = dbItem });
            }
        }

        public void AddEquipment(NetEquipment equip)
        {
            Equipment.Add(equip);
            if (dbChar != null)
            {
                equip.DbItem = new EquipmentEntity()
                {
                    EquipmentHardpoint = equip.Hardpoint,
                    EquipmentNickname = equip.Equipment.Nickname
                };
                dbChar.Character.Equipment.Add(equip.DbItem);
                dbChar.ApplyChanges();
            }   
        }
        public void RemoveEquipment(NetEquipment equip)
        {
            Equipment.Remove(equip);
            if (equip.DbItem != null)
            {
                dbChar.Character.Equipment.Remove(equip.DbItem);
                dbChar.ApplyChanges();
            }
        }

        

        public void RemoveCargo(NetCargo slot, int amount)
        {
            slot.Count -= amount;
            if (slot.Count <= 0)
            {
                Cargo.Remove(slot);
                if (slot.DbItem != null)
                {
                    dbChar.Character.Cargo.Remove(slot.DbItem);
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
            sl.Equipment = new List<NetShipEquip>(Equipment.Count);
            sl.Cargo = new List<NetShipCargo>(Cargo.Count);
            foreach(var equip in Equipment) {
                sl.Equipment.Add(new NetShipEquip(
                equip.Hardpoint == null ? 0 : CrcTool.FLModelCrc(equip.Hardpoint),
                    equip.Equipment.CRC,
                (byte)(equip.Health * 255f))); 
            }
            foreach (var c in Cargo) { 
                sl.Cargo.Add(new NetShipCargo(c.ID, c.Equipment.CRC, c.Count));
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
        public int Count;
        public CargoItem DbItem;
    }

    public class NetEquipment
    {
        public string Hardpoint;
        public GameData.Items.Equipment Equipment;
        public float Health;
        public EquipmentEntity DbItem;
    }
}