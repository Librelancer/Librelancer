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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
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

        protected override void SetParticle(int idx, NodeReference reference, ParticleEffectInstance instance, ref Matrix4 transform, float sparam, float globaltime)
		{
			var r_min = MinRadius.GetValue(sparam, 0);
			var r_max = MaxRadius.GetValue(sparam, 0);

			var radius = instance.Random.NextFloat(r_min, r_max);

			var p = new Vector3(
				instance.Random.NextFloat(-1, 1),
				instance.Random.NextFloat(-1, 1),
				instance.Random.NextFloat(-1, 1)
			);
			p.Normalize();
			var n = p;
			Vector3 translate;
            Quaternion rotate;
            if (DoTransform(reference, sparam, globaltime, out translate, out rotate)) {
                p += translate;
                n = rotate * n;
            }
			n *= Pressure.GetValue(sparam, 0);
			var pr = p * radius;
			instance.Particles[idx].Position = pr;
			instance.Particles[idx].Normal = n;
		}
	}
}

