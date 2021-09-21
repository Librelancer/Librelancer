// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.GameData.Items
{
    public class GunEquipment : Equipment
    {
        public Data.Equipment.Gun Def;
        public MunitionEquip Munition;

        static GunEquipment() => EquipmentObjectManager.RegisterType<GunEquipment>(AddEquipment);
        static GameObject AddEquipment(GameObject parent, ResourceManager res, EquipmentType type, string hardpoint, Equipment equip)
        {
            var gn = (GunEquipment) equip;
            var child = GameObject.WithModel(gn.ModelFile, type != EquipmentType.Server, res);
            if(type != EquipmentType.RemoteObject)
                child.Components.Add(new WeaponComponent(child, gn));
            return child;
        }
    }
}
