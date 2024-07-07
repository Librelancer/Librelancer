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

        static void ToEuler(Matrix4x4 mx, out float yaw, out float pitch, out float roll)
        {
            double p, y, r;
            double h = Math.Sqrt(mx.M11 * mx.M11 + mx.M12 * mx.M12);
            if (h > 0.00000001)
            {
                p = Math.Atan2( mx.M23, mx.M33);
                y = Math.Atan2(-mx.M13, h);
                r = Math.Atan2( mx.M12, mx.M11);
            }
            else
            {
                p = Math.Atan2(-mx.M32, mx.M22);
                y = Math.Atan2(-mx.M13, h);
                r = 0;
            }
            pitch = (float) p;
            yaw = (float) y;
            roll = (float) r;
        }

        static float Sanitize(float f) => Math.Abs(f) < float.Epsilon ? 0.0f : f;

        /// <summary>
        /// Gets the Pitch Yaw and Roll from a Matrix4x4 SLOW!!!
        /// </summary>
        /// <returns>(x - pitch, y - yaw, z - roll)</returns>
        /// <param name="mx">The matrix.</param>
        public static Vector3 GetEulerDegrees(this Matrix4x4 mx)
        {
            float p, y, r;
            ToEuler(mx, out y, out p, out r);
            const float radToDeg = 180.0f / MathF.PI;
            return new Vector3(Sanitize(p * radToDeg), Sanitize(y * radToDeg), Sanitize(r * radToDeg));
        }

        public static Vector3 GetEulerDegrees(this Quaternion q) =>
            GetEulerDegrees(Matrix4x4.CreateFromQuaternion(q));
    }
}

