using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Net.DataTypes;

[StructLayout(LayoutKind.Sequential)]
public struct Fix22d10 : IEquatable<Fix22d10>
{
    public int Value;

    public Fix22d10(float value)
    {
        Value = (int)MathHelper.Clamp((double)value * 1024.0, int.MinValue, int.MaxValue);
    }

    public float ToFloat()
    {
        return (float)((double)Value / 1024.0);
    }

    public bool Equals(Fix22d10 other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Fix22d10 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(Fix22d10 left, Fix22d10 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Fix22d10 left, Fix22d10 right)
    {
        return !left.Equals(right);
    }
}
