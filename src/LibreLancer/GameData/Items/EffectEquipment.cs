// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Client.Components;
using LibreLancer.Fx;
using LibreLancer.Render;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.GameData.Items
{
	public class EffectEquipment : Equipment
	{
		public ResolvedFx Particles;
		public EffectEquipment()
		{
		}
        static EffectEquipment() => EquipmentObjectManager.RegisterType<EffectEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var obj = new GameObject();
            if (type != EquipmentType.Server)
            {
                var e = (EffectEquipment) equip;
                if (e.Particles != null)
                {
                    obj.RenderComponent = new ParticleEffectRenderer(e.Particles.GetEffect(res));
                    obj.AddComponent(new CUpdateSParamComponent(obj));
                }
            }
            return obj;
        }
    }
}
