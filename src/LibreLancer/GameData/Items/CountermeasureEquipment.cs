// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Equipment;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.GameData.Items
{
    public class CountermeasureEquipment : Equipment
    {
        static CountermeasureEquipment() => EquipmentObjectManager.RegisterType<CountermeasureEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var sh = (CountermeasureEquipment)equip;
            var obj = GameObject.WithModel(sh.ModelFile, type != EquipmentType.Server, parent.Resources);
            return obj;
        }
    }
}