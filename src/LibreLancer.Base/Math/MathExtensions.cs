// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LibreLancer;

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

    private static void ToEuler(Matrix4x4 mx, out float yaw, out float pitch, out float roll)
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

    private static float Sanitize(float f) => Math.Abs(f) < float.Epsilon ? 0.0f : f;

    private static Matrix4x4 Orthonormalize(Matrix4x4 m)
    {
        Vector3 v0 = new Vector3(m.M11, m.M12, m.M13);
        Vector3 v1 = new Vector3(m.M21, m.M22, m.M23);
        Vector3 v2 = new Vector3(m.M31, m.M32, m.M33);

        Vector3 u0 = Vector3.Normalize(v0);

        Vector3 v1proj0 = Vector3.Dot(v1, u0) * u0;
        Vector3 u1 = Vector3.Normalize(v1 - v1proj0);

        Vector3 v2proj0 = Vector3.Dot(v2, u0) * u0;
        Vector3 v2proj1 = Vector3.Dot(v2, u1) * u1;
        Vector3 u2 = Vector3.Normalize(v2 - v2proj0 - v2proj1);

        Matrix4x4 result = m;
        result.M11 = u0.X; result.M12 = u0.Y; result.M13 = u0.Z;
        result.M21 = u1.X; result.M22 = u1.Y; result.M23 = u1.Z;
        result.M31 = u2.X; result.M32 = u2.Y; result.M33 = u2.Z;

        return result;
    }

    /// <summary>
    /// Gets the Pitch Yaw and Roll from a Matrix4x4 SLOW!!!
    /// </summary>
    /// <returns>(x - pitch, y - yaw, z - roll)</returns>
    /// <param name="mx">The matrix.</param>
    public static Vector3 GetEulerDegrees(this Matrix4x4 mx)
    {
        float p, y, r;
        ToEuler(Orthonormalize(mx), out y, out p, out r);
        const float radToDeg = 180.0f / MathF.PI;
        return new Vector3(Sanitize(p * radToDeg), Sanitize(y * radToDeg), Sanitize(r * radToDeg));
    }

    public static Vector3 GetEulerDegrees(this Quaternion q) =>
        GetEulerDegrees(Matrix4x4.CreateFromQuaternion(q));
}
