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
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxEmitter : FxNode
	{
		public int InitialParticles;
		public AlchemyCurveAnimation Frequency;
		public AlchemyCurveAnimation EmitCount;
		public AlchemyCurveAnimation InitLifeSpan;
		//public AlchemyCurveAnimation LODCurve; -- Not really relevant in a modern context
		public AlchemyCurveAnimation Pressure;
		public AlchemyCurveAnimation VelocityApproach;
		public AlchemyCurveAnimation MaxParticles;
		public FxEmitter (AlchemyNode ale) : base(ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter ("Emitter_InitialPartices", out temp)) {
				InitialParticles = (int)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_Frequency", out temp)) {
				Frequency = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_EmitCount", out temp)) {
				EmitCount = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_InitLifeSpan", out temp)) {
				InitLifeSpan = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_Pressure", out temp)) {
				Pressure = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_VelocityApproach", out temp)) {
				VelocityApproach = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("Emitter_MaxParticles", out temp)) {
				MaxParticles = (AlchemyCurveAnimation)temp.Value;
			}
		}

		public void SpawnInitialParticles(ParticleEffect fx, ParticleEffectInstance instance, ref Matrix4 transform, float sparam)
		{
			int j = 0;
			for (int i = 0; i < InitialParticles; i--)
			{
				var idx = instance.GetNextFreeParticle();
				if (idx == -1)
					break;
				SpawnParticle(idx, fx, instance, ref transform, sparam);
				j++;
			}
			instance.GetEmitterState(this).ParticleCount = j;
		}
		protected void SpawnParticle(int idx, ParticleEffect fx, ParticleEffectInstance instance, ref Matrix4 transform, float sparam)
		{
			instance.Particles[idx].Active = true;
			instance.Particles[idx].Emitter = this;
			instance.Particles[idx].Appearance = (FxAppearance)fx.Pairs[this][0];
			instance.Particles[idx].TimeAlive = 0f;
			instance.Particles[idx].LifeSpan = InitLifeSpan.GetValue(sparam, 0f);
			SetParticle(idx, fx, instance, ref transform, sparam);
		}
		protected virtual void SetParticle(int idx, ParticleEffect fx, ParticleEffectInstance instance, ref Matrix4 transform, float sparam)
		{
			
		}
		public override void Update(ParticleEffect fx, ParticleEffectInstance instance, TimeSpan delta, ref Matrix4 transform, float sparam)
		{
			var maxCount = MaxParticles == null ? int.MaxValue : (int)Math.Ceiling(MaxParticles.GetValue(sparam, 0f));
			var freq = Frequency == null ? 0f : Frequency.GetValue(sparam, 0f);
			var spawnMs = freq <= 0 ? 0 : 1 / (double)freq;
			var state = instance.GetEmitterState(this);
			if (state.ParticleCount >= maxCount)
			{
				return;
			}
			if (spawnMs > 0)
			{
				//Spawn lots of particles
				var dt = Math.Min(delta.TotalSeconds, 1); //don't go crazy during debug pauses
				while (true)
				{
					if (state.SpawnTimer < dt) {
						dt -= state.SpawnTimer;
						state.SpawnTimer = spawnMs;
					} else {
						state.SpawnTimer -= dt;
						break;
					}
					if (state.ParticleCount + 1 <= maxCount)
					{
						var idx = instance.GetNextFreeParticle();
						if (idx == -1)
							return;
						state.ParticleCount++;
						SpawnParticle(idx, fx, instance, ref transform, sparam);
						instance.Particles[idx].Appearance.OnParticleSpawned(idx, instance);
					}
				}
			}
		}
	}
}

