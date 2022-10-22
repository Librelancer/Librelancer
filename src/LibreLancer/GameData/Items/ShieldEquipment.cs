// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.GameData.Items
{
    public class ShieldEquipment : Equipment
    {
        public Data.Equipment.ShieldGenerator Def;
        static ShieldEquipment() => EquipmentObjectManager.RegisterType<ShieldEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var sh = (ShieldEquipment)equip;
            var obj = GameObject.WithModel(sh.ModelFile, type != EquipmentType.Server, parent.Resources);
            switch (type)
            {
                case EquipmentType.Server:
                    obj.Components.Add(new SShieldComponent(sh, obj));
                    break;
                case EquipmentType.LocalPlayer:
                case EquipmentType.RemoteObject:
                    obj.Components.Add(new CShieldComponent(sh, obj));
                    break;
            }
            return obj;
        }
    }
}