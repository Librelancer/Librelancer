// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.CompilerServices;
using LibreLancer.Sounds;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.GameData.Items
{
	public class PowerEquipment : Equipment
    {
        public Data.Equipment.PowerCore Def;
        public PowerEquipment()
		{
		}
        static PowerEquipment() => EquipmentObjectManager.RegisterType<PowerEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var pc = new PowerCoreComponent(((PowerEquipment)equip).Def, parent);
            parent.AddComponent(pc);
            return null;
        }
    }
}
