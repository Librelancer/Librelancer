using System;
using System.Linq;
namespace LibreLancer.Fx
{
	public class ParticleEffectInstance
	{
		const int PARTICLES_LIMIT = 1024;

		public Particle[] Particles;
		public ParticleEffect Effect;

		public ParticleEffectInstance (ParticleEffect fx)
		{
			Effect = fx;
			int emitterCount = 0;
			foreach (var node in Effect.Nodes) {
				if (node is FxEmitter)
					emitterCount++;
			}
			Particles = new Particle[PARTICLES_LIMIT * emitterCount];
		}

		public void Update(TimeSpan delta)
		{

		}

		public void Draw(RenderState rstate)
		{

		}
	}
}

