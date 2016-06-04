using System;
using OpenTK;
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
				Math.Pow((test.X / size.X), 2) +
				Math.Pow((test.Y / size.Y), 2) +
				Math.Pow((test.Z / size.Z), 2)
			);
			return (float)result;
		}
	}
}

