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
using System.Linq;
namespace LibreLancer.Fx
{
	public class EmitterState
	{
		public int ParticleCount = 0;
		public float SpawnTimer = 0f;
	}
	public class ParticleEffectInstance
	{
		public const int PARTICLES_PER_EMITTER = 1024;

		public Particle[] Particles;
		public ParticleEffect Effect;
		public ResourceManager Resources;
		Dictionary<FxEmitter, EmitterState> emitStates = new Dictionary<FxEmitter, EmitterState>();
		public Random Random = new Random();
		public ParticleEffectInstance (ParticleEffect fx)
		{
			Effect = fx;
			Particles = new Particle[PARTICLES_PER_EMITTER * fx.EmitterCount];
		}

		bool freeParticles;
		public void Update(TimeSpan delta, Matrix4 transform, float sparam)
		{
			freeParticles = true;
			for (int i = 0; i < Particles.Length; i++)
			{
				if (!Particles[i].Active)
					continue;
				Particles[i].Position += Particles[i].Normal * (float)delta.TotalSeconds;
				Particles[i].TimeAlive += (float)delta.TotalSeconds;
				if (Particles[i].TimeAlive >= Particles[i].LifeSpan)
				{
					Particles[i].Active = false;
					emitStates[Particles[i].Emitter].ParticleCount--;
					continue;
				}
			}

			Effect.Update(this, delta, ref transform, sparam);
		}

		public EmitterState GetEmitterState(FxEmitter emitter)
		{
			EmitterState es;
			if (!emitStates.TryGetValue(emitter, out es))
			{
				es = new EmitterState();
				emitStates.Add(emitter, es);
			}
			return es;
		}


		public int GetNextFreeParticle()
		{
			if (!freeParticles)
				return -1;
			for (int i = 0; i < Particles.Length; i++)
			{
				if (!Particles[i].Active)
					return i;
			}
			freeParticles = false;
			return -1;
		}

		public void Draw(Billboards billboards, Matrix4 transform, float sparam)
		{
			for (int i = 0; i < Particles.Length; i++)
			{
				if (!Particles[i].Active)
					continue;
				Particles[i].Appearance.Draw(ref Particles[i], Effect, Effect.ResourceManager, billboards, ref transform, sparam);
			}
		}
	}
}

