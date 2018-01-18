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
		Dictionary<NodeReference, EmitterState> emitStates = new Dictionary<NodeReference, EmitterState>();
		public Dictionary<NodeReference, LineBuffer> BeamAppearances = new Dictionary<NodeReference, LineBuffer>();
		public Dictionary<NodeReference, bool> EnableStates = new Dictionary<NodeReference, bool>();
		public Random Random = new Random();
		double globaltime = 0;
		public double GlobalTime
		{
			get
			{
				return globaltime;
			}
		}
		public ParticleEffectInstance (ParticleEffect fx)
		{
			Effect = fx;
			int emitterCount = 0;
			foreach (var node in fx.References)
			{
				if (node.Node is FLBeamAppearance)
				{
					BeamAppearances.Add(node, new LineBuffer(256));
				}
				if (node.Node is FxEmitter)
					emitterCount++;
			}
			Particles = new Particle[PARTICLES_PER_EMITTER * emitterCount];
		}

		public void Reset()
		{
			globaltime = 0;
			freeParticles = true;
			for (int i = 0; i < Particles.Length; i++)
			{
				Particles[i].Active = false;
			}
			foreach (var state in emitStates)
			{
				state.Value.SpawnTimer = 0;
				state.Value.ParticleCount = 0;
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
			//Update Emitters
			for (int i = 0; i < Effect.References.Count; i++)
			{
				var r = Effect.References[i];
				if (NodeEnabled(r) && (r.Node is FxEmitter))
				{
					((FxEmitter)r.Node).Update(r, this, delta, ref transform, sparam);
				}
			}
		}

		public EmitterState GetEmitterState(NodeReference emitter)
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

		public bool NodeEnabled(NodeReference node)
		{
			bool val;
			if (!EnableStates.TryGetValue(node, out val)) return true;
			return val;
		}

		public void Draw(PolylineRender polyline, Billboards billboards, PhysicsDebugRenderer debug, Matrix4 transform, float sparam)
		{
			for (int i = 0; i < Particles.Length; i++)
			{
				if (!Particles[i].Active)
					continue;
				if (NodeEnabled(Particles[i].Appearance))
				{
					var app = (FxAppearance)Particles[i].Appearance.Node;
					app.Debug = debug;
					app.Draw(ref Particles[i], (float)globaltime, Particles[i].Appearance, Effect.ResourceManager, billboards, ref transform, sparam);
				}
			}
			foreach (var kv in BeamAppearances)
			{
				if (NodeEnabled(kv.Key))
				{
					var app = (FLBeamAppearance)kv.Key.Node;
					app.DrawBeamApp(polyline, kv.Value, (float)globaltime, kv.Key, this, Effect.ResourceManager, billboards, ref transform, sparam);
				}
			}
		}
	}
}

