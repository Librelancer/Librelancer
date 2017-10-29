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
		Vector3 pt1;
		Vector3 pt2;
		Vector3 centre;
		float length_sq;
		float radius_sq;
		public ZoneCylinder(Zone zone, float r, float h) : base (zone)
		{
			Radius = r;
			Height = h;
			centre = Zone.Position;
			//Define the cylinder
			pt1 = Zone.Position - new Vector3(0, Height / 2, 0);
			pt2 = Zone.Position + new Vector3(0, Height / 2, 0);
			pt1 = Zone.RotationMatrix.Transform(pt1);
			pt2 = Zone.RotationMatrix.Transform(pt2);
			//Calculate values
			length_sq = VectorMath.DistanceSquared(pt1, pt2);
			radius_sq = Radius * Radius;
		}
		public override bool Intersects(BoundingBox box)
		{
			throw new NotImplementedException ();
		}
		public override bool ContainsPoint(Vector3 point)
		{
			Vector3 d = pt2 - pt1;
			Vector3 pd = point - pt1;
			float dot = Vector3.Dot (pd, d);

			if (dot < 0.0f || dot > length_sq)
			{
				return false;
			}
			else
			{
				float dsq = (pd.X * pd.X + pd.Y * pd.Y + pd.Z * pd.Z) - dot * dot / length_sq;
				return dsq <= radius_sq;
			}
		}
		public override ZoneShape Scale(float scale)
		{
			return new ZoneCylinder (Zone, Radius * scale, Height * scale);
		}
		public override float ScaledDistance(Vector3 point)
		{
			return VectorMath.Distance(point, centre) / Radius;
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			throw new NotImplementedException ();
		}
	}
}

