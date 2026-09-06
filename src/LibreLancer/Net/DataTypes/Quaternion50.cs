using System.Numerics;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net.DataTypes;

public record struct Quaternion50(uint Largest, ushort Component1, ushort Component2, ushort Component3)
{
    public static implicit operator Quaternion50(Quaternion q)
    {
        NetPacking.PackQuaternion(q, 16, out var lg, out var c1, out var c2, out var c3);
        return new(lg,(ushort)c1,(ushort)c2,(ushort)c3);
    }
    public Quaternion Quaternion => NetPacking.UnpackQuaternion(16, Largest, Component1, Component2, Component3);
}
