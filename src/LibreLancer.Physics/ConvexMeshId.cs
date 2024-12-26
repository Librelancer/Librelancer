using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Physics;

[StructLayout(LayoutKind.Explicit)]
public struct ConvexMeshId
{
    [FieldOffset(0)]
    internal ulong Bits;
    [FieldOffset(0)]
    public uint Id;
    [FieldOffset(4)]
    public uint SubId;

    public ConvexMeshId(uint id, uint subId)
    {
        Id = id;
        SubId = subId;
    }

    public override bool Equals(object obj) =>
        obj is ConvexMeshId other && Bits == other.Bits;

    public readonly bool Equals(ConvexMeshId other) =>
        Bits == other.Bits;

    public static bool operator ==(ConvexMeshId left, ConvexMeshId right) =>
        left.Bits == right.Bits;

    public static bool operator !=(ConvexMeshId left, ConvexMeshId right) =>
        left.Bits != right.Bits;

    public override readonly int GetHashCode() =>
        Bits.GetHashCode();

    internal ShapeId ShapeId(uint file) => new ShapeId(file, Bits);
}

internal record struct ShapeId(ulong File, ulong Mesh);
