using System;
using OpenTK;
using Jitter.LinearMath;
namespace LibreLancer
{
	public static class MatrixExtensions
	{
		public static Vector3 Transform(this Matrix4 mat, Vector3 toTransform)
		{
			return VectorMath.Transform (toTransform, mat);
		}
		public static void SetForward(this Matrix4 mat, Vector3 forward)
		{
			mat.M31 = -forward.X;
			mat.M32 = -forward.Y;
			mat.M33 = -forward.Z;
		}
		public static void SetUp(this Matrix4 mat, Vector3 up)
		{
			mat.M21 = up.X;
			mat.M22 = up.Y;
			mat.M23 = up.Z;
		}

		public static void SetRight(this Matrix4 mat, Vector3 right)
		{
			mat.M11 = right.X;
			mat.M12 = right.Y;
			mat.M13 = right.Z;
		}
		public static Vector3 GetForward(this Matrix4 mat)
		{
			return new Vector3 (-mat.M31, -mat.M32, -mat.M33);
		}
		public static Vector3 GetUp(this Matrix4 mat)
		{
			return new Vector3 (mat.M21, mat.M22, mat.M23);
		}
		public static Vector3 GetRight(this Matrix4 mat)
		{
			return new Vector3 (mat.M11, mat.M12, mat.M13);
		}
		public static Matrix4 ToOpenTK(this JMatrix src)
		{
			return new Matrix4 (
				         src.M11, src.M12, src.M13, 1,
				         src.M21, src.M22, src.M23, 1,
				         src.M31, src.M32, src.M33, 1,
				         0, 0, 0, 1
			         );
		}
	}
}

