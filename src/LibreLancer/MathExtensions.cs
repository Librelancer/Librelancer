// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public static class MathExtensions
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
			return new Vector3 (mat.M12, mat.M22, mat.M32);
		}
		public static Vector3 GetRight(this Matrix4 mat)
		{
			return new Vector3 (mat.M11, mat.M21, mat.M31);
		}
		/// <summary>
		/// Gets the Pitch Yaw and Roll from a Matrix3 SLOW!!!
		/// </summary>
		/// <returns>(x - pitch, y - yaw, z - roll)</returns>
		/// <param name="mx">The matrix.</param>
		public static Vector3 GetEuler(this Matrix4 mx)
		{
			double p, y, r;
			DecomposeOrientation(mx, out p, out y, out r);
			return new Vector3((float)p, (float)y, (float)r);
		}

        public static Vector3 GetEulerDegrees(this Matrix4 mx)
        {
            double p, y, r;
            DecomposeOrientation(mx, out p, out y, out r);
            const double radToDeg = 180.0 / Math.PI;
            return new Vector3((float)(p * radToDeg), (float)(y * radToDeg), (float)(r * radToDeg));
        }
		static void DecomposeOrientation(Matrix4 mx, out double xPitch, out double yYaw, out double zRoll)
		{
            double h = Math.Sqrt(mx.M11 * mx.M11 + mx.M21 * mx.M21);
            if(h > 1 / 524288.0) //Magic number
            {
                xPitch = Math.Atan2(mx.M32, mx.M33);
                yYaw = Math.Atan2(-mx.M31, h);
                zRoll = Math.Atan2(mx.M21, mx.M11);
            }
            else
            {
                xPitch = Math.Atan2(-mx.M23, mx.M22);
                yYaw = Math.Atan2(-mx.M31, h);
                zRoll = 0;
            }
        }
	}
}

