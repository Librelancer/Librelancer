// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Fx;
namespace LibreLancer.GameData.Items
{
	public class ThrusterEquipment : Equipment
	{
		public ResolvedFx Particles;
		public string HpParticles;
		public float Force;
		public float Drain;
        
		public ThrusterEquipment()
		{
		}
        
        static ThrusterEquipment() => EquipmentObjectManager.RegisterType<ThrusterEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, bool draw, string hardpoint, Equipment equip)
        {
            var th = (ThrusterEquipment)equip;
            var obj = GameObject.WithModel(th.ModelFile, draw, parent.Resources);
            obj.Components.Add(new ThrusterComponent(obj, th));
            return obj;
        }
    }
}
