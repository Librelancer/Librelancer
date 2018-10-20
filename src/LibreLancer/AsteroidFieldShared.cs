// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
		public static unsafe bool CubeExists(Vector3 cubePos, float empty_frequency, out float test_value)
		{
			//Check for fill rate
			test_value = 0;
			if (empty_frequency < float.Epsilon)
				return true;
			if (empty_frequency >= 1)
				return false;
			//integer hash
			var u = (uint*)&cubePos;
			var h = hash (u [0]) ^ hash (u [1]) ^ hash (u [2]);
			//get float
			test_value = constructFloat(h);
			return test_value < empty_frequency;
		}
		//return a float between [0,1] for a hash
		static unsafe float constructFloat(uint m)
		{
			const uint ieeeMantissa = 0x007FFFFFu;
			const uint ieeeOne = 0x3F800000u;
			m &= ieeeMantissa;
			m |= ieeeOne;
			float f = *(float*)&m;
			return f - 1.0f;
		}
		//simple hash function
		static uint hash(uint x)
		{
			x += (x << 10);
			x ^= (x >> 6);
			x += (x << 3);
			x ^= (x >> 11);
			x += (x << 15);
			return x;
		}
	}
}

