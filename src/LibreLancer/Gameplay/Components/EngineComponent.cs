/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using LibreLancer.GameData.Items;
using LibreLancer.Fx;
namespace LibreLancer
{
	public class EngineComponent : GameComponent
	{
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
		}
		public override void Update(TimeSpan time)
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Update(parent, time, Speed);
		}
		public override void Register(SystemRenderer renderer, Jitter.World physics)
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Effect.Register(renderer);
		}
		public override void Unregister()
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Effect.Unregister();
		}
		class AttachedEffect
		{
			public Hardpoint Attachment;
			public ParticleEffectRenderer Effect;
			public AttachedEffect(Hardpoint attachment, ParticleEffectRenderer fx)
			{
				Attachment = attachment;
				Effect = fx;
				Effect.SParam = 0.9f;
			}
			public void Update(GameObject parent, TimeSpan time, float sparam)
			{
				Effect.SParam = sparam;
				Effect.Update(time, Vector3.Zero, Attachment.Transform * parent.GetTransform());
			}
		}
	}
}
