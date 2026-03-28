// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client.Components
{
	public class CThrusterComponent : ThrusterComponent
	{
        private List<ParticleEffectRenderer> fireFx = [];
		public CThrusterComponent(GameObject parent, ThrusterEquipment equip) : base(parent, equip) { }

		public override void Update(double time, GameWorld world)
        {
            foreach (var renderer in fireFx)
            {
                renderer.Active = Enabled;
            }
        }

		public override void Register(GameWorld world)
        {
            if (GetGameData(world) != null)
            {
                var resman = GetResourceManager(world);
                var pfx = Equip.Particles?.GetEffect(resman!);
                foreach (var hp in Parent!.GetHardpoints()
                             .Where(x => x.Name.Equals(Equip.HpParticles, StringComparison.OrdinalIgnoreCase)))
                {
                    fireFx.Add(new ParticleEffectRenderer(pfx) { Attachment = hp, Active = false, SParam = 1 });
                }
            }

            foreach (var t in fireFx)
            {
                Parent!.ExtraRenderers.Add(t);
            }
        }

		public override void Unregister(GameWorld world)
        {
            foreach (var renderer in fireFx)
            {
                Parent!.ExtraRenderers.Remove(renderer);
            }
        }
    }
}
