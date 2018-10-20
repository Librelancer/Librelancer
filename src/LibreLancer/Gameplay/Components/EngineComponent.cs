// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.GameData.Items;
using LibreLancer.Fx;
namespace LibreLancer
{
	public class EngineComponent : GameComponent
	{
		public Engine Engine;
		public float Speed = 1f;
		List<AttachedEffect> fireFx = new List<AttachedEffect>();
		GameObject parent;
		public EngineComponent(GameObject parent, Engine engine, FreelancerGame game) : base(parent)
		{
			var fx = game.GameData.GetEffect(engine.FireEffect);
			var hps = parent.GetHardpoints();
			foreach (var hp in hps)
			{
				if (!hp.Name.Equals("hpengineglow", StringComparison.OrdinalIgnoreCase) &&
				    hp.Name.StartsWith("hpengine", StringComparison.OrdinalIgnoreCase))
				{
					fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(fx)));
				}
			}
			this.parent = parent;
			Engine = engine;
		}
		public override void Update(TimeSpan time)
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Update(parent, time, Speed);
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
