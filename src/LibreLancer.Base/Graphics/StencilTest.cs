using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics;

[StructLayout(LayoutKind.Explicit)]
public struct StencilTest(
    StencilFunction function,
    StencilOperation fail,
    StencilOperation depthFail,
    StencilOperation pass,
    int reference,
    uint mask) : IEquatable<StencilTest>
{
    public static readonly StencilTest Default = new(StencilFunction.Always, StencilOperation.Keep,
        StencilOperation.Keep, StencilOperation.Keep, 1, uint.MaxValue);
    [FieldOffset(0)] private uint Value;
    [FieldOffset(0)] public StencilFunction Function = function;
    [FieldOffset(1)] public StencilOperation Fail = fail;
    [FieldOffset(2)] public StencilOperation DepthFail = depthFail;
    [FieldOffset(3)] public StencilOperation Pass = pass;
    [FieldOffset(4)] public int Reference = reference;
    [FieldOffset(8)] public uint Mask = mask;

    public bool Equals(StencilTest other)
    {
        return Value == other.Value && Reference == other.Reference && Mask == other.Mask;
    }

    public override bool Equals(object? obj)
    {
        return obj is StencilTest other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)Value;
            hashCode = (hashCode * 397) ^ (int)Reference;
            hashCode = (hashCode * 397) ^ (int)Mask;
            return hashCode;
        }
    }

    public static bool operator ==(StencilTest left, StencilTest right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StencilTest left, StencilTest right)
    {
        return !left.Equals(right);
    }
}
