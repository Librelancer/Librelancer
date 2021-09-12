// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LibreLancer
{
    public class NetCharacter
    {
        public string Name;
        public string Base;
        public long Credits;
        public GameData.Ship Ship;
        public List<NetEquipment> Equipment;
        public List<NetCargo> Cargo;
        private long charId;
        GameDataManager gData;
        
        private int _itemID;
        public static NetCharacter FromDb(long id, GameServer game)
        {
            var character = game.Database.GetCharacter(id);
            
            var nc = new NetCharacter();
            nc.Name = character.Name;
            nc.gData = game.GameData;
            nc.charId = id;
            nc.Base = character.Base;
            nc.Ship = game.GameData.GetShip(character.Ship);
            nc.Credits = character.Money;
            nc.Equipment = new List<NetEquipment>(character.Equipment.Count);
            nc.Cargo = new List<NetCargo>();
            foreach(var equip in character.Equipment)
            {
                var resolved = game.GameData.GetEquipment(equip.EquipmentNickname);
                if (resolved == null) continue;
                nc.Equipment.Add(new NetEquipment()
                {
                    Hardpoint = equip.EquipmentHardpoint,
                    Equipment = resolved,
                    Health = 1f
                });
            }
            return nc;
        }

        public void AddCargo(GameData.Items.Equipment equip, int count)
        {
            var slot = Cargo.FirstOrDefault(x => equip.Good.Equipment == x.Equipment);
            if (slot == null)
            {
                Cargo.Add(new NetCargo() { Equipment =  equip, Count = count});
            }
            else
            {
                slot.Count += count;
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
    }

    public class NetEquipment
    {
        public string Hardpoint;
        public GameData.Items.Equipment Equipment;
        public float Health;
    }
}