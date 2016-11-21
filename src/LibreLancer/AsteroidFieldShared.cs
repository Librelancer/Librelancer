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

namespace LibreLancer
{
	public static class AsteroidFieldShared
	{
		//TODO: This function is disgusting (GetCloseCube)
		public static Vector3 GetCloseCube(Vector3 cameraPos, float cube_size)
		{
			var div = cameraPos / cube_size;
			var corner = new Vector3 (
				             (float)Math.Round (div.X), 
				             (float)Math.Round (div.Y), 
				             (float)Math.Round (div.Z)
			             ) * cube_size;
			var sz = new Vector3 (cube_size / 2f, cube_size / 2f, cube_size / 2f);
			//Find closest!
			var a = corner + new Vector3 (sz.X, sz.Y, sz.Z);
			var b = corner + new Vector3 (-sz.X, sz.Y, sz.Z);
			var c = corner + new Vector3 (sz.X, sz.Y, -sz.Z);
			var d = corner + new Vector3 (-sz.X, sz.Y, -sz.Z);

			var e = corner + new Vector3 (sz.X, -sz.Y, sz.Z);
			var f = corner + new Vector3 (-sz.X, -sz.Y, sz.Z);
			var g = corner + new Vector3 (sz.X, -sz.Y, -sz.Z);
			var h = corner + new Vector3 (-sz.X, -sz.Y, -sz.Z);

			float d2 = VectorMath.DistanceSquared (cameraPos, a);
			float temp;
			Vector3 result = a;

			if ((temp = VectorMath.DistanceSquared (cameraPos, b)) < d2) { d2 = temp; result = b; }
			if ((temp = VectorMath.DistanceSquared (cameraPos, c)) < d2) { d2 = temp; result = c; }
			if ((temp = VectorMath.DistanceSquared (cameraPos, d)) < d2) { d2 = temp; result = d; }
			if ((temp = VectorMath.DistanceSquared (cameraPos, e)) < d2) { d2 = temp; result = e; }
			if ((temp = VectorMath.DistanceSquared (cameraPos, f)) < d2) { d2 = temp; result = f; }
			if ((temp = VectorMath.DistanceSquared (cameraPos, g)) < d2) { d2 = temp; result = g; }
			if ((temp = VectorMath.DistanceSquared (cameraPos, h)) < d2) { d2 = temp; result = h; }

			return result;
		}
		//TODO: This function seems to work, but should probably be analyzed to see if the outputs are any good
		/// <summary>
		/// Function to determine whether or not a cube is present in a field based on fill_rate
		/// </summary>
		/// <returns><c>true</c> if the cube is present, <c>false</c> otherwise.</returns>
		/// <param name="cubePos">Cube position.</param>
		/// <param name="fill_rate">Fill rate.</param>
		public static bool CubeExists(Vector3 cubePos, float empty_frequency)
		{
			//Check for fill rate
			if (empty_frequency < float.Epsilon)
				return true;
			if (empty_frequency >= 1)
				return false;
			//Create the 128-bit Seed
			ulong s0 = ((ulong)cubePos.X << 32) | ((ulong)cubePos.Y);
			ulong s1 = ((ulong)cubePos.Z << 32) | (ulong)(uint)((double)empty_frequency * uint.MaxValue);
			//XORSHIFT it 3 times
			ulong rand = 0;
			for (int i = 0; i < 3; i++) {
				ulong x = s0;
				ulong y = s1;
				s0 = y;
				x ^= x << 23;
				s1 = x ^ y ^ (x >> 17) ^ (y >> 26);
				rand = s1 + y;
			}
			//Make a floating point number
			var n = (double)rand / (double)UInt64.MaxValue;
			return n > empty_frequency;
		}
	}
}

