using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Physics;

[StructLayout(LayoutKind.Explicit)]
public struct ConvexShapeId(uint id, uint subId)
{
    [FieldOffset(0)]
    internal ulong Bits;
    [FieldOffset(0)]
    public uint Id = id;
    [FieldOffset(4)]
    public uint SubId = subId;

    public override bool Equals(object? obj) =>
        obj is ConvexShapeId other && Bits == other.Bits;

    public readonly bool Equals(ConvexShapeId other) =>
        Bits == other.Bits;

    public static bool operator ==(ConvexShapeId left, ConvexShapeId right) =>
        left.Bits == right.Bits;

    public static bool operator !=(ConvexShapeId left, ConvexShapeId right) =>
        left.Bits != right.Bits;

    public override readonly int GetHashCode() =>
        Bits.GetHashCode();

    internal ShapeId ShapeId(uint file) => new ShapeId(file, Bits);
}

internal record struct ShapeId(ulong File, ulong Mesh);
