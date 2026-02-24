// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
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
            MinRadius =  ale.GetCurveAnimation(AleProperty.ConeEmitter_MinRadius);
            MaxRadius =  ale.GetCurveAnimation(AleProperty.ConeEmitter_MaxRadius);
            MinSpread =  ale.GetCurveAnimation(AleProperty.ConeEmitter_MinSpread);
            MaxSpread =  ale.GetCurveAnimation(AleProperty.ConeEmitter_MaxSpread);
		}

        public FxConeEmitter(string name) : base(name)
        {
            MinRadius = new (1);
            MaxRadius = new (1);
            MinSpread = new (0);
            MaxSpread = new (0);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.ConeEmitter_MinRadius, MinRadius));
            n.Parameters.Add(new(AleProperty.ConeEmitter_MaxRadius, MaxRadius));
            n.Parameters.Add(new(AleProperty.ConeEmitter_MinSpread, MinSpread));
            n.Parameters.Add(new(AleProperty.ConeEmitter_MaxSpread, MaxSpread));
            return n;
        }

        protected virtual float GetSpread(float sparam, float time)
		{
			var s_min = MathHelper.DegreesToRadians(MinSpread.GetValue(sparam, 0));
			var s_max = MathHelper.DegreesToRadians(MaxSpread.GetValue(sparam, 0));
			return FxRandom.NextFloat(s_min, s_max);
		}

        protected override void SetParticle(EmitterReference reference, ref Particle particle, float sparam, float globaltime)
		{
			var r_min = MinRadius.GetValue(sparam, 0);
			var r_max = MaxRadius.GetValue(sparam, 0);

			var radius = FxRandom.NextFloat(r_min, r_max);
			float s_min = MathHelper.DegreesToRadians(MinSpread.GetValue(sparam, 0));
			float s_max = MathHelper.DegreesToRadians(MaxSpread.GetValue(sparam, 0));

			var n = RandomInCone(s_min, s_max);
            Vector3 translate;
            Quaternion rotate;
            if (DoTransform(reference, sparam, globaltime, out translate, out rotate))
            {
                n = Vector3.Transform(n, rotate);
            }
            var p = n * radius + translate;
			n *= Pressure.GetValue(sparam, 0);
            particle.Position = p;
            particle.Normal = n;
		}

		//Different direction to FxCubeEmitter
        static Vector3 RandomInCone(float minspread, float maxspread)
		{
			var direction = Vector3.UnitY;
            var axis = Vector3.UnitX;

			var angle = FxRandom.NextFloat(minspread, maxspread);
			var rotation = Quaternion.CreateFromAxisAngle(axis, angle);
            Vector3 output = Vector3.Transform(direction, rotation);
			var random = FxRandom.NextFloat(-MathF.PI, MathF.PI);
			rotation = Quaternion.CreateFromAxisAngle(direction, random);
            output = Vector3.Transform(output, rotation);
			return output;
		}
	}
}
