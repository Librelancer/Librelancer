// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer
{
    public class NetCharacter
    {
        public string Name;
        public string Base;
        public long Credits;
        public GameData.Ship Ship;
        public List<NetEquipment> Equipment;

        private long charId;
        GameDataManager gData;
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

        public NetShipLoadout EncodeLoadout()
        {
            var sl = new NetShipLoadout();
            sl.ShipCRC = Ship.CRC;
            sl.Equipment = new List<NetShipEquip>(Equipment.Count);
            foreach(var equip in Equipment) {
                sl.Equipment.Add(new NetShipEquip(
                equip.Hardpoint == null ? 0 : CrcTool.FLModelCrc(equip.Hardpoint),
                    equip.Equipment.CRC,
                (byte)(equip.Health * 255f))); 
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

    public class NetEquipment
    {
        public string Hardpoint;
        public GameData.Items.Equipment Equipment;
        public float Health;
    }
}