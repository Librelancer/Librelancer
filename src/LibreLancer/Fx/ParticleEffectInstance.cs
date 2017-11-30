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
		public double SpawnTimer = 0f;
	}
	public class ParticleEffectInstance
	{
		public const int PARTICLES_PER_EMITTER = 1024;

		public Particle[] Particles;
		public ParticleEffect Effect;
		public ResourceManager Resources;
		Dictionary<FxEmitter, EmitterState> emitStates = new Dictionary<FxEmitter, EmitterState>();
		public Dictionary<FLBeamAppearance, LineBuffer> BeamAppearances = new Dictionary<FLBeamAppearance, LineBuffer>();
		public Dictionary<string, bool> EnableStates = new Dictionary<string, bool>();
		public Random Random = new Random();
		double globaltime = 0;
		public ParticleEffectInstance (ParticleEffect fx)
		{
			Effect = fx;
			Particles = new Particle[PARTICLES_PER_EMITTER * fx.EmitterCount];
			foreach (var node in fx.Nodes)
			{
				if (node is FLBeamAppearance) {
					var beam = (FLBeamAppearance)node;
					BeamAppearances.Add(beam, new LineBuffer(256));
				}
			}
		}

		bool freeParticles;
		public void Update(TimeSpan delta, Matrix4 transform, float sparam)
		{
			globaltime += delta.TotalSeconds;
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
			//Clean the circular buffers
			foreach (var buffer in BeamAppearances.Values)
			{
				for (int i = 0; i < buffer.Count; i++)
				{
					if (buffer[i].Active && Particles[buffer[i].ParticleIndex].Active == false)
						buffer[i] = new LinePointer() { ParticleIndex = -256, Active = false };
				}
				while (buffer.Count > 0 && buffer.Peek().Active == false)
					buffer.Dequeue();
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
				if (!Particles[i].Active) {
					return i;
				}
			}
			freeParticles = false;
			return -1;
		}

		bool DrawEnabled(FxAppearance node)
		{
			bool val;
			if (!EnableStates.TryGetValue(node.NodeName, out val)) return true;
			return val;
		}

		public void Draw(PolylineRender polyline, Billboards billboards, PhysicsDebugRenderer debug, Matrix4 transform, float sparam)
		{
			for (int i = 0; i < Particles.Length; i++)
			{
				if (!Particles[i].Active)
					continue;
				if (DrawEnabled(Particles[i].Appearance))
				{
					Particles[i].Appearance.Debug = debug;
					Particles[i].Appearance.Draw(ref Particles[i], (float)globaltime, Effect, Effect.ResourceManager, billboards, ref transform, sparam);
				}
			}
			foreach (var kv in BeamAppearances)
			{
				if (DrawEnabled(kv.Key))
				{
					kv.Key.DrawBeamApp(polyline, kv.Value, (float)globaltime, Effect, this, Effect.ResourceManager, billboards, ref transform, sparam);
				}
			}
		}
	}
}

