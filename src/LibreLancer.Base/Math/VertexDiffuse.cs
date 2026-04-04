using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace LibreLancer;

[StructLayout(LayoutKind.Explicit)]
public struct VertexDiffuse(byte r, byte g, byte b, byte a)
{
    public static readonly VertexDiffuse White = (VertexDiffuse)0xFFFFFFFF;

    [FieldOffset(0)]
    public uint Pixel;

    [FieldOffset(0)]
    public byte R = r;

    [FieldOffset(1)]
    public byte G = g;

    [FieldOffset(2)]
    public byte B = b;

    [FieldOffset(3)]
    public byte A = a;

    public static implicit operator uint(VertexDiffuse diffuse) => diffuse.Pixel;

    public static explicit operator VertexDiffuse(Vector4 color)
    {
        var clamped = Vector128.MinNative(
            Vector128.MaxNative((color * 255).AsVector128(), Vector128<float>.Zero),
            Vector128.Create<float>(255));
        var ints = Vector128.ConvertToInt32Native(clamped);
        var shorts = Vector128.Narrow(ints, ints);
        var bytes = Vector128.Narrow(shorts, shorts)
            .AsUInt32();
        return (VertexDiffuse)bytes.ToScalar();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator VertexDiffuse(Color4 color) =>
        (VertexDiffuse)Unsafe.BitCast<Color4, Vector4>(color);

    public static explicit operator Color4(VertexDiffuse diffuse) =>
        new Color4(diffuse.R / 255f, diffuse.G / 255f, diffuse.B / 255f, diffuse.A / 255f);

    public static explicit operator VertexDiffuse(uint source)
    {
        return new VertexDiffuse() { Pixel = source };
    }

    public string ToString(string format) => Pixel.ToString(format);

    public override string ToString() => Pixel.ToString("X8");
}
