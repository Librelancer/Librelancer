using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.Net.DataTypes;

[StructLayout(LayoutKind.Sequential)]
public struct Vec3Fix22d10 : IEquatable<Vec3Fix22d10>
{
    public Fix22d10 X;
    public Fix22d10 Y;
    public Fix22d10 Z;

    public Vec3Fix22d10(Vector3 v)
    {
        X = new(v.X);
        Y = new(v.Y);
        Z = new(v.Z);
    }

    public Vector3 ToVector3() => new(X.ToFloat(), Y.ToFloat(), Z.ToFloat());

    public static Vec3Fix22d10 operator +(Vec3Fix22d10 v1, Vec3Fix22d10 v2) => new()
    {
        X = new Fix22d10() { Value = v1.X.Value + v2.X.Value },
        Y = new Fix22d10() { Value = v1.Y.Value + v2.Y.Value },
        Z = new Fix22d10() { Value = v1.Z.Value + v2.Z.Value }
    };

    public static Vec3Fix22d10 operator -(Vec3Fix22d10 v1, Vec3Fix22d10 v2) => new()
    {
        X = new Fix22d10() { Value = v1.X.Value - v2.X.Value },
        Y = new Fix22d10() { Value = v1.Y.Value - v2.Y.Value },
        Z = new Fix22d10() { Value = v1.Z.Value - v2.Z.Value }
    };

    public static bool operator ==(Vec3Fix22d10 left, Vec3Fix22d10 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vec3Fix22d10 left, Vec3Fix22d10 right)
    {
        return !left.Equals(right);
    }

    public bool Equals(Vec3Fix22d10 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vec3Fix22d10 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}
