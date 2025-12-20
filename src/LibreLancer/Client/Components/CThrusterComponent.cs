// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client.Components
{
	public class CThrusterComponent : ThrusterComponent
	{
        List<ParticleEffectRenderer> fireFx = new List<ParticleEffectRenderer>();
		public CThrusterComponent(GameObject parent, ThrusterEquipment equip) : base(parent, equip) { }

		public override void Update(double time)
		{
            for (int i = 0; i < fireFx.Count; i++)
            {
                fireFx[i].Active = Enabled;
            }
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
                    fireFx.Add(new ParticleEffectRenderer(pfx) { Attachment = hp, Active = false, SParam = 1 });
                }
            }

            for (int i = 0; i < fireFx.Count; i++)
                Parent.ExtraRenderers.Add(fireFx[i]);
        }
		public override void Unregister(Physics.PhysicsWorld physics)
        {
            for (int i = 0; i < fireFx.Count; i++)
                Parent.ExtraRenderers.Remove(fireFx[i]);
        }
    }
}
