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

namespace LibreLancer.GameData
{
	public class ZoneBox : ZoneShape
	{
		public Vector3 Size;
		Matrix4 R;
		Vector3 transformedPos;
		public ZoneBox (Zone zone, float x, float y, float z) : base(zone)
		{
			Size = new Vector3 (x, y, z);
			R = zone.RotationMatrix;
			R.Transpose();
			transformedPos = R.Transform(zone.Position);
		}
		public override bool Intersects(BoundingBox box)
		{
			var min = Zone.Position - (Size / 2);
			var max = Zone.Position + (Size / 2);
			var me = new BoundingBox (min, max);
			return me.Intersects (box);
		}
		public override bool ContainsPoint(Vector3 point)
		{
			//transform point
			point = R.Transform(point) - transformedPos;
			//test
			var min = -(Size * 0.5f);
			var max = (Size * 0.5f);
			return !(point.X < min.X || point.Y < min.Y || point.Z < min.Z || point.X > max.X || point.Y > max.Y || point.Z > max.Z);
		}
		public override ZoneShape Scale(float scale)
		{
			var scl = Size * scale;
			return new ZoneBox(Zone, scl.X, scl.Y, scl.Z);
		}
		public override float ScaledDistance(Vector3 point)
		{
			var max = Math.Max(Math.Max(Size.X, Size.Y), Size.Z);
			return VectorMath.Distance(transformedPos, point) / max;
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			return new Vector3 (randfunc (), randfunc (), randfunc ()) * Size;
		}

	}
}

