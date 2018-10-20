// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxConeEmitter : FxEmitter
	{
		public AlchemyCurveAnimation MinRadius;
		public AlchemyCurveAnimation MaxRadius;
		public AlchemyCurveAnimation MinSpread;
		public AlchemyCurveAnimation MaxSpread;

		public FxConeEmitter (AlchemyNode ale) : base(ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter("ConeEmitter_MinRadius", out temp)) {
				MinRadius = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("ConeEmitter_MaxRadius", out temp)) {
				MaxRadius = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("ConeEmitter_MinSpread", out temp)){
				MinSpread = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("ConeEmitter_MaxSpread", out temp)) {
				MaxSpread = (AlchemyCurveAnimation)temp.Value;
			}
		}

		protected virtual float GetSpread(Random rand, float sparam, float time)
		{
			var s_min = MathHelper.DegreesToRadians(MinSpread.GetValue(sparam, 0));
			var s_max = MathHelper.DegreesToRadians(MaxSpread.GetValue(sparam, 0));
			return rand.NextFloat(s_min, s_max);
		}

        protected override void SetParticle(int idx, NodeReference reference, ParticleEffectInstance instance, ref Matrix4 transform, float sparam, float globaltime)
		{
			var r_min = MinRadius.GetValue(sparam, 0);
			var r_max = MaxRadius.GetValue(sparam, 0);

			var radius = instance.Random.NextFloat(r_min, r_max);
			float s_min = MathHelper.DegreesToRadians(MinSpread.GetValue(sparam, 0));
			float s_max = MathHelper.DegreesToRadians(MaxSpread.GetValue(sparam, 0));

			var n = RandomInCone(instance.Random, s_min, s_max);
            Vector3 translate;
            Quaternion rotate;
            if (DoTransform(reference, sparam, globaltime, out translate, out rotate))
            {
                n = rotate * n;
            }
            var p = n * radius + translate;
			n *= Pressure.GetValue(sparam, 0);
			instance.Particles[idx].Position = p;
			instance.Particles[idx].Normal = n;
		}

		//Different direction to FxCubeEmitter
        static Vector3 RandomInCone(Random r, float minspread, float maxspread)
		{
			var direction = Vector3.UnitY;
            var axis = Vector3.UnitX;

			var angle = r.NextFloat(minspread, maxspread);
			var rotation = Quaternion.FromAxisAngle(axis, angle);
			Vector3 output = rotation * direction;
			var random = r.NextFloat(-MathHelper.Pi, MathHelper.Pi);
			rotation = Quaternion.FromAxisAngle(direction, random);
			output = rotation * output;
			return output;
		}
	}
}