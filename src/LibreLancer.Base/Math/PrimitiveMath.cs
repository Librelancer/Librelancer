// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
namespace LibreLancer
{
	public static class PrimitiveMath
	{
		//Standard equation of an ellipsoid: (x/a)^2 + (y/b)^2 + (z/c)^2 = 1
		public static bool EllipsoidContains(Vector3 center, Vector3 size, Vector3 point)
		{
			return EllipsoidFunction(center, size, point) <= 1;
		}
		public static float EllipsoidFunction(Vector3 center, Vector3 size, Vector3 point)
		{
			var test = point - center;
            return (test / size).LengthSquared();
        }
		public static Vector3 GetPointOnRadius(Vector3 size, float y, float angle)
		{
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

