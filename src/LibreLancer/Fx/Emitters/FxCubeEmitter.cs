// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxCubeEmitter : FxEmitter
	{
		public AlchemyCurveAnimation Width;
		public AlchemyCurveAnimation Height;
		public AlchemyCurveAnimation Depth;
		public AlchemyCurveAnimation MinSpread;
		public AlchemyCurveAnimation MaxSpread;

		public FxCubeEmitter (AlchemyNode ale) : base(ale)
        {
            Width = ale.GetCurveAnimation(AleProperty.CubeEmitter_Width);
            Height = ale.GetCurveAnimation(AleProperty.CubeEmitter_Height);
            Depth = ale.GetCurveAnimation(AleProperty.CubeEmitter_Depth);
            MinSpread = ale.GetCurveAnimation(AleProperty.CubeEmitter_MinSpread);
            MaxSpread = ale.GetCurveAnimation(AleProperty.CubeEmitter_MaxSpread);
		}

        public FxCubeEmitter(string name) : base(name)
        {
            Width = new (1);
            Height = new (1);
            Depth = new (1);
            MinSpread = new (0);
            MaxSpread = new (0);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.CubeEmitter_Width, Width));
            n.Parameters.Add(new(AleProperty.CubeEmitter_Height, Height));
            n.Parameters.Add(new(AleProperty.CubeEmitter_Depth, Depth));
            n.Parameters.Add(new(AleProperty.CubeEmitter_MinSpread, MinSpread));
            n.Parameters.Add(new(AleProperty.CubeEmitter_MaxSpread, MaxSpread));
            return n;
        }

        protected override void SetParticle(EmitterReference reference, ref Particle particle, float sparam, float globaltime)
		{
			float w = Width.GetValue (sparam, 0) / 2;
			float h = Height.GetValue (sparam, 0) / 2;
			float d = Depth.GetValue (sparam, 0) / 2;
			float s_min = MathHelper.DegreesToRadians (MinSpread.GetValue (sparam, 0));
			float s_max = MathHelper.DegreesToRadians (MaxSpread.GetValue (sparam, 0));

			var pos = new Vector3 (
				          FxRandom.NextFloat (-w, w),
				          FxRandom.NextFloat (-h, h),
				          FxRandom.NextFloat (-d, d)
			          );
			var n = RandomInCone(s_min, s_max);
            Vector3 translate;
            Quaternion rotate;
            if(DoTransform(reference, sparam, globaltime, out translate, out rotate)) {
                pos += translate;
                n = Vector3.Transform(n, rotate);
            }
			var pr = pos;
			particle.Position = pr;
			particle.Normal = n * Pressure.GetValue (sparam, 0);
		}

        //Different direction to FxCubeEmitter
        static Vector3 RandomInCone(float minspread, float maxspread)
        {
            return Vector3.UnitY;
            var direction = Vector3.UnitY;
            var axis = Vector3.UnitZ;

            var angle = FxRandom.NextFloat(minspread, maxspread);
            var rotation = Quaternion.CreateFromAxisAngle(axis, angle);
            Vector3 output = Vector3.Transform(direction, rotation);
            var random = FxRandom.NextFloat(-MathF.PI, MathF.PI);
            rotation = Quaternion.CreateFromAxisAngle(direction, random);
            output = Vector3.Transform(output, rotation);
            return output;
        }

        static Vector3 RandomCube(float minspread, float maxspread)
		{
			//(sqrt(1 - z^2) * cosϕ, sqrt(1 - z^2) * sinϕ, z)
            var halfspread = maxspread / 2;

			float z = FxRandom.NextFloat((float)Math.Cos(halfspread), 1 - (minspread / 2));
			float t = FxRandom.NextFloat(0, (float)(Math.PI * 2));
            return new Vector3(
                (float)(Math.Sqrt(1 - z * z) * Math.Cos(t)),
                (float)(Math.Sqrt(1 - z * z) * Math.Sin(t)),
                z
            );
		}

	}
}

