// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Fx;
namespace LibreLancer.GameData.Items
{
	public class EffectEquipment : Equipment
	{
		public ResolvedFx Particles;
		public EffectEquipment()
		{
		}
        static EffectEquipment() => EquipmentObjectManager.RegisterType<EffectEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, bool draw, string hardpoint, Equipment equip)
        {
            var obj = new GameObject();
            if (draw)
            {
                obj.RenderComponent = new ParticleEffectRenderer(((EffectEquipment) equip).Particles.GetEffect(res));
                obj.Components.Add(new UpdateSParamComponent(obj));
            }
            return obj;
        }
    }
}
