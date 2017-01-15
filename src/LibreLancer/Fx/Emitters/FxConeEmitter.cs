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
		protected override void SetParticle(int idx, ParticleEffect fx, ParticleEffectInstance instance, ref Matrix4 transform, float sparam)
		{

			var r_min = MinRadius.GetValue(sparam, 0);
			var r_max = MaxRadius.GetValue(sparam, 0);

			var s_min = MathHelper.DegreesToRadians(MinSpread.GetValue(sparam, 0));
			var s_max = MathHelper.DegreesToRadians(MaxSpread.GetValue(sparam, 0));

			var radius = instance.Random.NextFloat(r_min, r_max);
			var theta = instance.Random.NextFloat(s_min, s_max);
			var phi = instance.Random.NextFloat(s_min, s_max);

			var x = Math.Sin(phi) * Math.Cos(theta);
			var y = Math.Sin(phi) * Math.Sin(theta);
			var z = Math.Cos(phi);

			var p = new Vector3(
				(float)x,
				(float)y,
				(float)z
			);

			var tr = Transform.GetMatrix(sparam, 0);
			var direction = Vector3.UnitZ + p;
			var n = (tr * new Vector4(direction.Normalized(), 0)).Xyz.Normalized();
			//var pressure = Pressure.GetValue(sparam, 0);
			n *= Pressure.GetValue(sparam, 0);
			var pr = p * radius;
			instance.Particles[idx].Position = pr;
			instance.Particles[idx].Normal = n;
		}
	}
}