// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.GameData.Items
{
    public class ShieldEquipment : Equipment
    {
        static ShieldEquipment() => EquipmentObjectManager.RegisterType<ShieldEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, EquipmentType type, string hardpoint, Equipment equip)
        {
            var sh = (ShieldEquipment)equip;
            var obj = GameObject.WithModel(sh.ModelFile, type != EquipmentType.Server, parent.Resources);
            return obj;
        }
    }
}