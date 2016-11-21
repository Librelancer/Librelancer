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
	public class ZoneBox : ZoneShape
	{
		public Vector3 Size;
		public ZoneBox (float x, float y, float z)
		{
			Size = new Vector3 (x, y, z);
		}
		public override bool Intersects(Vector3 position, BoundingBox box)
		{
			var min = position - (Size / 2);
			var max = position + (Size / 2);
			var me = new BoundingBox (min, max);
			return me.Intersects (box);
		}
		public override bool ContainsPoint(Vector3 position, Matrix4 rotation, Vector3 point)
		{
			var min = position - (Size / 2);
			var max = position + (Size / 2);
			return !(point.X < min.X || point.Y < min.Y || point.Z < min.Z || point.X > max.X || point.Y > max.Y || point.Z > max.Z);
		}
		public override ZoneShape Scale(float scale)
		{
			var scl = Size * scale;
			return new ZoneBox(scl.X, scl.Y, scl.Z);
		}
		public override float ScaledDistance(Vector3 position, Vector3 point)
		{
			throw new NotImplementedException ();
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			return new Vector3 (randfunc (), randfunc (), randfunc ()) * Size;
		}

	}
}

