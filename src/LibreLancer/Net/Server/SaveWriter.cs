// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using System.Text;
using LibreLancer.Data.Save;

namespace LibreLancer
{
    public static class SaveWriter
    {
        public static string WriteSave(NetCharacter ch, string description, int ids, DateTime? timeStamp)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Player]");
            if (timeStamp != null)
            {
                var fileTime = timeStamp?.ToFileTime();
                builder.Append("tstamp = ");
                builder.Append((fileTime >> 32).ToString());
                builder.Append(", ");
                builder.AppendLine((fileTime & 0xFFFFFFFF).ToString());
            }
            if (description != null)
                builder.Append("description = ").AppendLine(SavePlayer.EncodeName(description));
            else
                builder.Append("descrip_strid = ").AppendLine(ids.ToString());
            if(!string.IsNullOrWhiteSpace(ch.Name))
                builder.Append("name = ").AppendLine(SavePlayer.EncodeName(ch.Name));
            if (!string.IsNullOrWhiteSpace(ch.Base))
                builder.Append("base = ").AppendLine(ch.Base);
            if (!string.IsNullOrWhiteSpace(ch.System))
                builder.Append("system = ").AppendLine(ch.System);
            builder.Append("position = ").AppendLine(Vector3(ch.Position));
            builder.Append("money = ").AppendLine(ch.Credits.ToString());
            if (ch.Ship != null)
                builder.Append("ship_archetype = ").AppendLine(ch.Ship.Nickname);
            foreach (var equipment in ch.Equipment)
            {
                builder.Append("equip = ").Append(equipment.Equipment.Nickname).Append(",")
                    .Append(equipment.Hardpoint ?? "").Append(",").AppendLine("1");
            }
            foreach (var cargo in ch.Cargo)
            {
                builder.Append("cargo = ");
                builder.AppendLine($"{cargo.Equipment.Nickname}, {cargo.Count}, , , 0");
            }
            return builder.ToString();
        }

        static string Float(float f) => f.ToString("0.#########");
        static string Vector3(Vector3 v) => $"{Float(v.X)}, {Float(v.Y)}, {Float(v.Z)}";
    }
}