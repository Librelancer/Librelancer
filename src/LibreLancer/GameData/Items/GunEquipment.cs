// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Client.Components;
using LibreLancer.Sounds;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.GameData.Items
{
    public class GunEquipment : Equipment
    {
        public Data.Equipment.Gun Def;
        public MunitionEquip Munition;
        public ResolvedFx FlashEffect;

        static GunEquipment() => EquipmentObjectManager.RegisterType<GunEquipment>(AddEquipment);
        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var gn = (GunEquipment) equip;
            var child = GameObject.WithModel(gn.ModelFile, type != EquipmentType.Server, res);
            if(type != EquipmentType.RemoteObject &&
               type != EquipmentType.Cutscene)
                child.AddComponent(new GunComponent(child, gn));
            if(type == EquipmentType.LocalPlayer ||
               type == EquipmentType.RemoteObject)
                child.AddComponent(new CMuzzleFlashComponent(child, gn));
            if (snd != null)
            {
                snd.LoadSound(gn.Munition.Def.OneShotSound);
            }
            return child;
        }
    }
}
