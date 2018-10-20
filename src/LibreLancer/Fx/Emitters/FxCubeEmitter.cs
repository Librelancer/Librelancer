// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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
			AleParameter temp;
			if (ale.TryGetParameter ("CubeEmitter_Width", out temp)) {
				Width = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("CubeEmitter_Height", out temp)) {
				Height = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("CubeEmitter_Depth", out temp)) {
				Depth = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("CubeEmitter_MinSpread", out temp)){
				MinSpread = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("CubeEmitter_MaxSpread", out temp)) {
				MaxSpread = (AlchemyCurveAnimation)temp.Value;
			}	
		}

        protected override void SetParticle (int idx, NodeReference reference, ParticleEffectInstance instance, ref Matrix4 transform, float sparam, float globaltime)
		{
			float w = Width.GetValue (sparam, 0) / 2;
			float h = Height.GetValue (sparam, 0) / 2;
			float d = Depth.GetValue (sparam, 0) / 2;
			float s_min = MathHelper.DegreesToRadians (MinSpread.GetValue (sparam, 0));
			float s_max = MathHelper.DegreesToRadians (MaxSpread.GetValue (sparam, 0));

			var pos = new Vector3 (
				          instance.Random.NextFloat (-w, w),
				          instance.Random.NextFloat (-h, h),
				          instance.Random.NextFloat (-d, d)
			          );
			var n = RandomInCone(instance.Random, s_min, s_max);
            //var tr = Transform.GetMatrix(sparam, globaltime);
            //var tr = Matrix4.Identity;
            //n = (tr * new Vector4(n.Normalized(), 0)).Xyz.Normalized();
            Vector3 translate;
            Quaternion rotate;
            if(DoTransform(reference, sparam, globaltime, out translate, out rotate)) {
                pos += translate;
                n = rotate * n;
            }
			var pr = pos;
			instance.Particles[idx].Position = pr;
			instance.Particles [idx].Normal = n * Pressure.GetValue (sparam, 0);
		}

        //Different direction to FxCubeEmitter
        static Vector3 RandomInCone(Random r, float minspread, float maxspread)
        {
            return Vector3.UnitY;
            var direction = Vector3.UnitY;
            var axis = Vector3.UnitZ;

            var angle = r.NextFloat(minspread, maxspread);
            var rotation = Quaternion.FromAxisAngle(axis, angle);
            Vector3 output = rotation * direction;
            var random = r.NextFloat(-MathHelper.Pi, MathHelper.Pi);
            rotation = Quaternion.FromAxisAngle(direction, random);
            output = rotation * output;
            return output;
        }

        static Vector3 RandomCube(Random r, float minspread, float maxspread)
		{
			//(sqrt(1 - z^2) * cosϕ, sqrt(1 - z^2) * sinϕ, z)
            var halfspread = maxspread / 2;

			float z = r.NextFloat((float)Math.Cos(halfspread), 1 - (minspread / 2));
			float t = r.NextFloat(0, (float)(Math.PI * 2));
            return new Vector3(
                (float)(Math.Sqrt(1 - z * z) * Math.Cos(t)),
                (float)(Math.Sqrt(1 - z * z) * Math.Sin(t)),
                z
            );
		}

	}
}

