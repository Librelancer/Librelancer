// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class FxSphereEmitter : FxEmitter
	{
		public AlchemyCurveAnimation MinRadius;
		public AlchemyCurveAnimation MaxRadius;

		public FxSphereEmitter (AlchemyNode ale) : base(ale)
        {
            MinRadius = ale.GetCurveAnimation(AleProperty.SphereEmitter_MinRadius)!;
            MaxRadius = ale.GetCurveAnimation(AleProperty.SphereEmitter_MaxRadius)!;
		}

        public FxSphereEmitter(string name) : base(name)
        {
            MinRadius = new(1);
            MaxRadius = new(1);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.SphereEmitter_MinRadius, MinRadius));
            n.Parameters.Add(new(AleProperty.SphereEmitter_MaxRadius, MaxRadius));
            return n;
        }

        protected override void SetParticle(ParticleEffectInstance instance, EmitterReference reference, ref Particle particle, float sparam, float globaltime)
		{
			var r_min = MinRadius.GetValue(sparam, 0);
			var r_max = MaxRadius.GetValue(sparam, 0);

			var radius = FxRandom.NextFloat(r_min, r_max);

			var p = new Vector3(
				FxRandom.NextFloat(-1, 1),
				FxRandom.NextFloat(-1, 1),
				FxRandom.NextFloat(-1, 1)
			);
			p.Normalize();
			var n = p;

            ref Matrix4x4 particleSpawnTransform = ref instance.EmitterParticleSpawnTransforms[reference.EmitterNodeIdx];
            if (!particleSpawnTransform.IsIdentity)
            {
                //TODO: Opimize
                p=Vector3.Transform(p, particleSpawnTransform);
                n=Vector3.TransformNormal(n, particleSpawnTransform);
            }

			n *= Pressure.GetValue(sparam, 0);
			var pr = p * radius;
			particle.Position = pr;
			particle.Velocity = n;
		}
	}
}

