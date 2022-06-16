// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer
{
	public class CThrusterComponent : ThrusterComponent
	{
        List<AttachedEffect> fireFx = new List<AttachedEffect>();
		public CThrusterComponent(GameObject parent, GameData.Items.ThrusterEquipment equip) : base(parent, equip) { }

		public override void Update(double time)
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Update(Parent, time, Enabled ? 1 : 0);
		}
		public override void Register(Physics.PhysicsWorld physics)
        {
            GameDataManager gameData;
            if ((gameData = GetGameData()) != null)
            {
                var resman = GetResourceManager();
                var pfx = Equip.Particles.GetEffect(resman);
                foreach (var hp in Parent.GetHardpoints()
                             .Where(x => x.Name.Equals(Equip.HpParticles, StringComparison.OrdinalIgnoreCase)))
                {
                    fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(pfx)));
                }
            }

            for (int i = 0; i < fireFx.Count; i++)
                Parent.ExtraRenderers.Add(fireFx[i].Effect);
		}
		public override void Unregister(Physics.PhysicsWorld physics)
		{
			for (int i = 0; i < fireFx.Count; i++)
                Parent.ExtraRenderers.Remove(fireFx[i].Effect);
		}
    }
}
