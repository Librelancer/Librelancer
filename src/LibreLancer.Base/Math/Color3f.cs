// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer;

public struct Color3f : IEquatable<Color3f>
{
    public bool Equals(Color3f other)
    {
        return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B);
    }

    public override bool Equals(object? obj)
    {
        return obj is Color3f other && Equals(other);
    }

    public static bool operator ==(Color3f left, Color3f right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color3f left, Color3f right)
    {
        return !left.Equals(right);
    }

    public static readonly Color3f White = new Color3f(1, 1, 1);
    public static readonly Color3f Black = new Color3f(0, 0, 0);

    public float R;
    public float G;
    public float B;

    public Color3f(float r, float g, float b)
    {
        R = r;
        G = g;
        B = b;
    }

    public Color3f(Vector3 val) : this(val.X, val.Y, val.Z) {}
    public Color4 ToColor4() => new Color4(R, G, B, 1.0f);

    public override string ToString ()
    {
        return $"[R:{R}, G:{G}, B:{B}]";
    }

    public static Color3f operator *(Color3f a, Color3f b)
    {
        return new Color3f(
            a.R * b.R,
            a.G * b.G,
            a.B * b.B
        );
    }

    public static Color3f operator +(Color3f a, Color3f b)
    {
        return new Color3f(
            a.R + b.R,
            a.G + b.G,
            a.B + b.B
        );
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B);
    }
}
