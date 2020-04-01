// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LibreLancer
{
	public static class MathExtensions
	{
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 ClearTranslation(this Matrix4x4 self)
        {
            var mat = self;
            mat.M41 = 0;
            mat.M42 = 0;
            mat.M43 = 0;
            return mat;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(ref this Vector3 vec)
        {
            vec = Vector3.Normalize(vec);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalized(this Vector3 vec)
        {
            return Vector3.Normalize(vec);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ExtractRotation(this Matrix4x4 mat)
        {
            Matrix4x4.Decompose(mat, out _, out var rot, out _);
            return rot;
        }
        public static Vector3 GetForward(this Matrix4x4 mat)
		{
			return new Vector3 (-mat.M31, -mat.M32, -mat.M33);
		}

		public static Vector3 GetUp(this Matrix4x4 mat)
		{
			return new Vector3 (mat.M12, mat.M22, mat.M32);
		}
		public static Vector3 GetRight(this Matrix4x4 mat)
		{
			return new Vector3 (mat.M11, mat.M21, mat.M31);
		}
		/// <summary>
		/// Gets the Pitch Yaw and Roll from a Matrix3 SLOW!!!
		/// </summary>
		/// <returns>(x - pitch, y - yaw, z - roll)</returns>
		/// <param name="mx">The matrix.</param>
		public static Vector3 GetEuler(this Matrix4x4 mx)
		{
			double p, y, r;
			DecomposeOrientation(mx, out p, out y, out r);
			return new Vector3((float)p, (float)y, (float)r);
		}

        public static Vector3 GetEulerDegrees(this Matrix4x4 mx)
        {
            double p, y, r;
            DecomposeOrientation(mx, out p, out y, out r);
            const double radToDeg = 180.0 / Math.PI;
            return new Vector3((float)(p * radToDeg), (float)(y * radToDeg), (float)(r * radToDeg));
        }
		static void DecomposeOrientation(Matrix4x4 mx, out double xPitch, out double yYaw, out double zRoll)
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

