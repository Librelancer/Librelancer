using System.Numerics;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net.DataTypes;

public record struct Quaternion32(uint Largest, ushort Component1, ushort Component2, ushort Component3)
{
    public static implicit operator Quaternion32(Quaternion q)
    {
        var uq = new Quaternion32();
        NetPacking.PackQuaternion(q, 10, out var lg, out var c1, out var c2, out var c3);
        return new(lg,(ushort)c1,(ushort)c2,(ushort)c3);
    }

    public Quaternion Quaternion => NetPacking.UnpackQuaternion(10, Largest, Component1, Component2, Component3);
}
