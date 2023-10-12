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
			AleParameter temp;
			if (ale.TryGetParameter("SphereEmitter_MinRadius", out temp))
			{
				MinRadius = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("SphereEmitter_MaxRadius", out temp))
			{
				MaxRadius = (AlchemyCurveAnimation)temp.Value;
			}
		}

        protected override void SetParticle(EmitterReference reference, ref Particle particle, float sparam, float globaltime)
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
			Vector3 translate;
            Quaternion rotate;
            if (DoTransform(reference, sparam, globaltime, out translate, out rotate)) {
                p += translate;
                n = Vector3.Transform(n, rotate);
            }
			n *= Pressure.GetValue(sparam, 0);
			var pr = p * radius;
			particle.Position = pr;
			particle.Normal = n;
		}
	}
}

