// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;

namespace LibreLancer
{
	public static class PrimitiveMath
	{
		//Standard equation of an ellipsoid: (x/a)^2 + (y/b)^2 + (z/c)^2 = 1
		public static bool EllipsoidContains(Vector3 center, Vector3 size, Vector3 point)
		{
			return EllipsoidFunction(center, size, point) < 1;
		}
		public static float EllipsoidFunction(Vector3 center, Vector3 size, Vector3 point)
		{
			var test = point - center;
			double result = (
				((test.X / size.X) * (test.X / size.X)) +
				((test.Y / size.Y) * (test.Y / size.Y)) +
				((test.Z / size.Z) * (test.Z / size.Z))
			);
			return (float)result;
		}
		public static Vector3 GetPointOnRadius(Vector3 size, float y, float angle)
		{
			/*float x0 = (float)Math.Sin(angle);
			float z0 = (float)Math.Cos(angle);

			float scalefactor = 1 - (y / size.Y);

			float x = x0 * size.X * scalefactor;
			float z = z0 * size.X * scalefactor;
			return new Vector3(x, y, z);*/

			//sphere:
			//r = sqrt(R^2 - y^2)
			var y_rel = y - (size.Y / 2);
			var r = Math.Sqrt(size.Y * size.Y - y_rel * y_rel);
			var x = Math.Cos(angle) * r;
			var z = Math.Sin(angle) * r;
			//map to ellipsoid:
			return new Vector3(
				(float)(x * (size.X / size.Y)), 
				y, 
				(float)(z * (size.Z / size.Y))
			);
		}
	}
}

