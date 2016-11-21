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
	public class ZoneCylinder : ZoneShape
	{
		public float Radius;
		public float Height;
		public ZoneCylinder(float r, float h)
		{
			Radius = r;
			Height = h;
		}
		public override bool Intersects(Vector3 position, BoundingBox box)
		{
			throw new NotImplementedException ();
		}
		public override bool ContainsPoint(Vector3 position, Matrix4 rotation, Vector3 point)
		{
			//Define the cylinder
			var pt1 = position - new Vector3 (0, Height / 2, 0);
			var pt2 = position + new Vector3 (0, Height / 2, 0);
			pt1 = rotation.Transform (pt1);
			pt2 = rotation.Transform (pt2);
			//Calculate values
			var length_sq = VectorMath.DistanceSquared (pt1, pt2);
			var radius_sq = Radius * Radius;
			Vector3 d = pt2 - pt1;
			Vector3 pd = point - pt1;
			float dot = Vector3.Dot (pd, d);

			return !(dot < 0.0f || dot > length_sq);
		}
		public override ZoneShape Scale(float scale)
		{
			return new ZoneCylinder (Radius * scale, Height * scale);
		}
		public override float ScaledDistance(Vector3 position, Vector3 point)
		{
			throw new NotImplementedException ();
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			throw new NotImplementedException ();
		}
	}
}

