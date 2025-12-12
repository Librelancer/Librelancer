#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 *
 * Contributions by Andy Gill, James Talton and Georg Wächter.
 */
#endregion

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace LibreLancer
{
    /// <summary>
    /// Contains common mathematical functions and constants.
    /// </summary>
    public static class MathHelper
    {
        public const float TwoPi = MathF.PI * 2.0f;

        public const float PiOver2 = MathF.PI / 2.0f;

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
        public static float DegreesToRadians(float degrees)
        {
            const float degToRad = (float)System.Math.PI / 180.0f;
            return degrees * degToRad;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="radians">An angle in radians</param>
        /// <returns>The angle expressed in degrees</returns>
        public static float RadiansToDegrees(float radians)
        {
            const float radToDeg = 180.0f / (float)System.Math.PI;
            return radians * radToDeg;
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
        public static double DegreesToRadians(double degrees)
        {
            const double degToRad = System.Math.PI / 180.0;
            return degrees * degToRad;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="radians">An angle in radians</param>
        /// <returns>The angle expressed in degrees</returns>
        public static double RadiansToDegrees(double radians)
        {
            const double radToDeg = 180.0 / System.Math.PI;
            return radians * radToDeg;
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clamp<T>(T n, T min, T max) where T : IBinaryNumber<T>
        {
            return n < min ? min : n > max ? max : n;
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float n, float min, float max)
        {
            if (Sse.IsSupported)
            {
                return Sse.MinScalar(
                        Sse.MaxScalar(Vector128.CreateScalarUnsafe(n), Vector128.CreateScalarUnsafe(min)),
                        Vector128.CreateScalarUnsafe(max))
                    .ToScalar();
            }
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.MinNumberScalar(
                    AdvSimd.MaxNumberScalar(Vector64.CreateScalarUnsafe(n), Vector64.CreateScalarUnsafe(min)),
                    Vector64.CreateScalarUnsafe(max)).ToScalar();
            }
            return Clamp<float>(n, min, max);
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double n, double min, double max)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.MinScalar(
                        Sse2.MaxScalar(Vector128.CreateScalarUnsafe(n), Vector128.CreateScalarUnsafe(min)),
                        Vector128.CreateScalarUnsafe(max))
                    .ToScalar();
            }
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.MinNumberScalar(
                    AdvSimd.MaxNumberScalar(Vector64.CreateScalarUnsafe(n), Vector64.CreateScalarUnsafe(min)),
                    Vector64.CreateScalarUnsafe(max)).ToScalar();
            }
            return Clamp<double>(n, min, max);
        }


        public static float Lerp(float value1, float value2, float amount)
        {
            if (Fma.IsSupported || AdvSimd.IsSupported)
                return MathF.FusedMultiplyAdd(amount, value2 - value1, value1);
            else
                return value1 + (value2 - value1) * amount;
        }

        public static float Snap(float s, float step)
        {
            if (step != 0f)
            {
                return MathF.Floor((s / step) + 0.5f) * step;
            }
            return s;
        }

        public static Vector2 Snap(Vector2 vector, Vector2 step) =>
            new Vector2(Snap(vector.X, step.X), Snap(vector.Y, step.Y));

        public static bool IsPowerOfTwo(int x)
        {
            if (x == 0) return false;
            return (x & (x - 1)) == 0;
        }

        public static float WrapF(float x, float max)
        {
            return (max + x % max) % max;
        }

        public static float QuatError(Quaternion a, Quaternion b)
        {
            if (a.W < 0) a = -a;
            if (b.W < 0) b = -b;
            var errorQuat = 1 - Quaternion.Dot(a, b);
            return errorQuat < float.Epsilon ? 0 : errorQuat;
        }

        public static float WrapF(float x, float min, float max)
        {
            return min + WrapF(x - min, max - min);
        }

        public static Vector3 ApplyEpsilon(Vector3 input, float epsilon = 0.0001f)
        {
            var output = input;
            if (Math.Abs(output.X) < epsilon) output.X = 0;
            if (Math.Abs(output.Y) < epsilon) output.Y = 0;
            if (Math.Abs(output.Z) < epsilon) output.Z = 0;
            return output;
        }

        public static Matrix4x4 MatrixFromEulerDegrees(Vector3 angles)
        {
            angles *= (MathF.PI / 180.0f);
            return  Matrix4x4.CreateRotationX(angles.X) *
                    Matrix4x4.CreateRotationY(angles.Y) *
                    Matrix4x4.CreateRotationZ(angles.Z);
        }

        public static Quaternion QuatFromEulerDegrees(Vector3 angles) =>
            MatrixFromEulerDegrees(angles).ExtractRotation();

        public static Quaternion QuatFromEulerDegrees(float x, float y, float z)
        {
            return MatrixFromEulerDegrees(x, y, z).ExtractRotation();
            //Not equivalent?
            /*x *= MathF.PI / 180.0f;
            y *= MathF.PI / 180.0f;
            z *= MathF.PI / 180.0f;
            return Quaternion.CreateFromAxisAngle(Vector3.UnitX, x) *
                   Quaternion.CreateFromAxisAngle(Vector3.UnitY, y) *
                   Quaternion.CreateFromAxisAngle(Vector3.UnitZ, z);*/
        }
        public static Matrix4x4 MatrixFromEulerDegrees(float x, float y, float z)
        {
            x *= MathF.PI / 180.0f;
            y *= MathF.PI / 180.0f;
            z *= MathF.PI / 180.0f;
            return  Matrix4x4.CreateRotationX(x) *
                    Matrix4x4.CreateRotationY(y) *
                    Matrix4x4.CreateRotationZ(z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetFlag(int flags, int idx) =>
            (flags & (1 << idx)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag(ref int flags, int idx, bool value)
        {
            if (value)
                flags |= (1 << idx);
            else
                flags &= ~(1 << idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag<T>(ref T source, T flag) where T : struct, Enum
        {
            var underlying = typeof(T).GetEnumUnderlyingType();
            if (underlying == typeof(byte) || underlying == typeof(sbyte))
            {
                var flagB = Unsafe.BitCast<T, byte>(flag);
                ref var sourceB = ref Unsafe.As<T, byte>(ref source);
                sourceB |= flagB;
            }
            if (underlying == typeof(short) || underlying == typeof(ushort))
            {
                var flagS = Unsafe.BitCast<T, ushort>(flag);
                ref var sourceS = ref Unsafe.As<T, ushort>(ref source);
                sourceS |= flagS;
            }
            if (underlying == typeof(int) || underlying == typeof(uint))
            {
                var flagI = Unsafe.BitCast<T, uint>(flag);
                ref var sourceI = ref Unsafe.As<T, uint>(ref source);
                sourceI |= flagI;
            }
            if (underlying == typeof(long) || underlying == typeof(ulong))
            {
                var flagL = Unsafe.BitCast<T, ulong>(flag);
                ref var sourceL = ref Unsafe.As<T, ulong>(ref source);
                sourceL |= flagL;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsetFlag<T>(ref T source, T flag) where T : struct, Enum
        {
            var underlying = typeof(T).GetEnumUnderlyingType();
            if (underlying == typeof(byte) || underlying == typeof(sbyte))
            {
                var flagB = Unsafe.BitCast<T, byte>(flag);
                ref var sourceB = ref Unsafe.As<T, byte>(ref source);
                sourceB &= (byte)~flagB;
            }
            if (underlying == typeof(short) || underlying == typeof(ushort))
            {
                var flagS = Unsafe.BitCast<T, ushort>(flag);
                ref var sourceS = ref Unsafe.As<T, ushort>(ref source);
                sourceS &= (ushort)~flagS;
            }
            if (underlying == typeof(int) || underlying == typeof(uint))
            {
                var flagI = Unsafe.BitCast<T, uint>(flag);
                ref var sourceI = ref Unsafe.As<T, uint>(ref source);
                sourceI &= ~flagI;
            }
            if (underlying == typeof(long) || underlying == typeof(ulong))
            {
                var flagL = Unsafe.BitCast<T, ulong>(flag);
                ref var sourceL = ref Unsafe.As<T, ulong>(ref source);
                sourceL &= ~flagL;
            }
        }

    }
}
