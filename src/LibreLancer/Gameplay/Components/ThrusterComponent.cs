// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class ThrusterComponent : GameComponent
	{
		public GameData.Items.ThrusterEquipment Equip;
		public bool Enabled;
		List<AttachedEffect> fireFx = new List<AttachedEffect>();
		public ThrusterComponent(GameObject parent, GameData.Items.ThrusterEquipment equip) : base(parent)
		{
			Equip = equip;
			var hps = parent.GetHardpoints();
			foreach (var hp in hps)
			{
				if (!hp.Name.Equals(Equip.HpParticles, StringComparison.OrdinalIgnoreCase))
				{
					fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(Equip.Particles)));
				}
			}
		}

		public override void Update(TimeSpan time)
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Update(Parent, time, Enabled ? 1 : 0);
		}
		public override void Register(Physics.PhysicsWorld physics)
		{
			for (int i = 0; i < fireFx.Count; i++)
                Parent.ForceRenderCheck.Add(fireFx[i].Effect);
		}
		public override void Unregister(Physics.PhysicsWorld physics)
		{
			for (int i = 0; i < fireFx.Count; i++)
                Parent.ForceRenderCheck.Remove(fireFx[i].Effect);
		}
	}
}
