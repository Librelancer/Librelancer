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

namespace LibreLancer.GameData
{
	public class ZoneSphere : ZoneShape
	{
		public float Radius;
		public ZoneSphere(float r)
		{
			Radius = r;
		}
		public override bool Intersects(Vector3 position, BoundingBox box)
		{
			var sph = new BoundingSphere(position, Radius);
			return sph.Intersects(box);
		}
		public override bool ContainsPoint(Vector3 position, Matrix4 rotation, Vector3 point)
		{
			return new BoundingSphere(position, Radius).Contains(point) != ContainmentType.Disjoint;
		}
		public override ZoneShape Scale(float scale)
		{
			return new ZoneSphere(Radius * scale);
		}
		public override float ScaledDistance(Vector3 position, Vector3 point)
		{
			return VectorMath.Distance(position, point) / Radius;
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			var theta = randfunc () * 2 * Math.PI;
			var phi = randfunc () * 2 * Math.PI;
			var x = Math.Cos (theta) * Math.Cos (phi);
			var y = Math.Sin (phi);
			var z = Math.Sin (theta) * Math.Cos (phi);
			return new Vector3 ((float)x, (float)y, (float)z) * Radius;
		}
	}
}

