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
        public IDrawable Model;
      
        public override IDrawable GetDrawable()
        {
            return Model;
        }

        static GunEquipment() => EquipmentObjectManager.RegisterType<GunEquipment>(AddEquipment);
        static GameObject AddEquipment(GameObject parent, ResourceManager res, string hardpoint, Equipment equip)
        {
            var gn = (GunEquipment) equip;
            var child = GameObject.WithModel(gn.Model, res);
            child.Components.Add(new WeaponComponent(child, gn));
            return child;
        }
    }
}
