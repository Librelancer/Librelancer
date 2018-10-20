// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Equipment
{
    public abstract class AbstractEquipment
    {
        private Section section;

        public string Nickname { get; private set; }
        public float[] LODRanges { get; private set; }
        public string HPChild { get; private set; }
        protected AbstractEquipment(Section section)
        {
            if (section == null) throw new ArgumentNullException("section");

            this.section = section;
        }

        protected bool parentEntry(Entry e)
        {
            switch (e.Name.ToLowerInvariant())
            {
                case "nickname":
                    if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
                    if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
                    Nickname = e[0].ToString();
                    break;
                case "lodranges":
                    LODRanges = new float[e.Count];
                    for (int i = 0; i < e.Count; i++) LODRanges[i] = e[i].ToSingle();
                    break;
                case "hp_child":
                    HPChild = e[0].ToString();
                    break;
                default: return false;
            }

            return true;
        }
    }
}
