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

		protected override void SetParticle (int idx, ParticleEffect fx, ParticleEffectInstance instance, ref Matrix4 transform, float sparam)
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
			var n = Vector3.UnitY;
			var theta = instance.Random.NextFloat(s_min, s_max);
			var phi = instance.Random.NextFloat(s_min, s_max);
			n += new Vector3(
				(float)(Math.Sin(phi) * Math.Cos(theta)),
				(float)(Math.Sin(phi) * Math.Sin(theta)),
				(float)(Math.Cos(phi))
			);

			var tr = GetTranslation(fx, transform, sparam, 0);
			n = (tr * new Vector4(n.Normalized(), 0)).Xyz.Normalized();
			var pr = tr.Transform(pos);
			instance.Particles[idx].Position = pr;
			instance.Particles [idx].Normal = n * Pressure.GetValue (sparam, 0);
		}

	}
}

