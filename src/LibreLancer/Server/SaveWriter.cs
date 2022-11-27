// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.Save;

namespace LibreLancer.Server
{
    public static class SaveWriter
    {
        public static string WriteSave(NetCharacter ch, string description, int ids, DateTime? timeStamp)
        {
            var sg = new SaveGame();
            sg.Player = new SavePlayer();
            sg.Player.TimeStamp = timeStamp;
            if (description != null)
                sg.Player.Description = description;
            else
                sg.Player.DescripStrid = ids;
            sg.Player.Name = ch.Name;
            sg.Player.Base = ch.Base;
            sg.Player.System = ch.System;
            sg.Player.Position = ch.Position;
            sg.Player.Money = ch.Credits;
            if (ch.Ship != null)
                sg.Player.ShipArchetype = new HashValue(ch.Ship.Nickname);
            foreach (var item in ch.Items) {
                if (!string.IsNullOrEmpty(item.Hardpoint))
                {
                    sg.Player.Equip.Add(new PlayerEquipment()
                    {
                        Item = new HashValue(item.Equipment.CRC),
                        Hardpoint = item.Hardpoint.Equals("internal", StringComparison.OrdinalIgnoreCase)
                            ? ""
                            : item.Hardpoint
                    });
                }
                else
                {
                    sg.Player.Cargo.Add(new PlayerCargo() {
                        Item = new HashValue(item.Equipment.CRC),
                        Count = item.Count
                    });
                }
            }
            foreach (var rep in ch.Reputation.Reputations) {
                sg.Player.House.Add(new SaveRep() { Group = rep.Key.Nickname, Reputation = rep.Value });
            }
            sg.Player.Interface = 3; //Unknown, matching vanilla
            
            return sg.ToString();
        }

        static string Float(float f) => f.ToString("0.#########");
        static string Vector3(Vector3 v) => $"{Float(v.X)}, {Float(v.Y)}, {Float(v.Z)}";
    }
}