// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Equipment
{
    public class Gun : AbstractEquipment
    {
        public string DaArchetype;
        public string MaterialLibrary;
        public int IdsName;
        public int IdsInfo;
        public int Hitpoints;
        public float TurnRate;

        public Gun(Section section)
            : base(section)
        {
            foreach (Entry e in section)
            {
                if (!parentEntry(e))
                {
                    switch (e.Name.ToLowerInvariant())
                    {
                        case "ids_name":
                            IdsName = e[0].ToInt32();
                            break;
                        case "ids_info":
                            IdsInfo = e[0].ToInt32();
                            break;
                        case "da_archetype":
                            DaArchetype = e[0].ToString();
                            break;
                        case "material_library":
                            MaterialLibrary = e[0].ToString();
                            break;
                        case "hit_pts":
                            Hitpoints = e[0].ToInt32();
                            break;
                        case "turn_rate":
                            TurnRate = e[0].ToSingle();
                            break;
                    }
                }
            }
        }
    }
}
