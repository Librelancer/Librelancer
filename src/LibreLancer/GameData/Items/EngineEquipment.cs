// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.GameData.Items
{
	public class EngineEquipment : Equipment
    {
        static EngineEquipment() => EquipmentObjectManager.RegisterType<EngineEquipment>(AddEquipment);
        static GameObject AddEquipment(GameObject parent, ResourceManager res, EquipmentType type, string hardpoint, Equipment equip)
        {
            if(type != EquipmentType.Cutscene)
                parent.Components.Add(new CEngineComponent(parent, (EngineEquipment)equip));
            return null;
        }
        public Data.Equipment.Engine Def;
    }
}
