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

        GameDataManager gData;
        public static NetCharacter FromDb(ServerCharacter character, GameDataManager gameData)
        {
            var nc = new NetCharacter();
            nc.Name = character.Name;
            nc.gData = gameData;
            nc.Base = character.Base;
            nc.Ship = gameData.GetShip(character.Ship);
            nc.Credits = character.Credits;
            nc.Equipment = new List<NetEquipment>(character.Equipment.Count);
            foreach(var equip in character.Equipment)
            {
                nc.Equipment.Add(new NetEquipment()
                {
                    Hardpoint = equip.Hardpoint,
                    Equipment = gameData.GetEquipment(equip.Equipment),
                    Health = equip.Health
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