using LibreLancer.Net.Protocol;

namespace LibreLancer.Net.DataTypes;

public record struct Norm12(uint Value)
{
    public static implicit operator Norm12(float f) => new(NetPacking.QuantizeFloat(f, 0, 1, 12));
    public static explicit operator float(Norm12 n) => NetPacking.UnquantizeFloat(n.Value, 0, 1, 12);

    public uint GetDelta(Norm12 other) => NetPacking.ZigZagDelta(Value, other.Value);

    public static Norm12 ApplyDelta(Norm12 other, uint delta) => new(NetPacking.ApplyZigZagDelta(other.Value, delta));
}
