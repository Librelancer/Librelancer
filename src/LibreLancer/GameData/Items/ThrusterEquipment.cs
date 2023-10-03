// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Client.Components;
using LibreLancer.Fx;
using LibreLancer.Sounds;
using LibreLancer.World;
using LibreLancer.World.Components;

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

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var th = (ThrusterEquipment)equip;
            var obj = GameObject.WithModel(th.ModelFile, type != EquipmentType.Server, parent.Resources);
            if(type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
                obj.AddComponent(new CThrusterComponent(obj, th));
            else if (type == EquipmentType.Server)
                obj.AddComponent(new ThrusterComponent(obj, th));
            return obj;
        }
    }
}
