using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer;

[StructLayout(LayoutKind.Explicit)]
public struct VertexDiffuse(byte r, byte g, byte b, byte a)
{
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

    public static explicit operator VertexDiffuse(Color4 color)
    {
        var a = (byte) MathHelper.Clamp(color.A * 255, 0, 255);
        var r = (byte)MathHelper.Clamp(color.R * 255, 0, 255);
        var g = (byte)MathHelper.Clamp(color.G * 255, 0, 255);
        var b = (byte)MathHelper.Clamp(color.B * 255, 0, 255);
        return new VertexDiffuse(r, g, b, a);
    }

    public static explicit operator Color4(VertexDiffuse diffuse) =>
        new Color4(diffuse.R / 255f, diffuse.G / 255f, diffuse.B / 255f, diffuse.A / 255f);

    public static explicit operator VertexDiffuse(Vector4 color) => (VertexDiffuse)(Color4)color;

    public static explicit operator VertexDiffuse(uint source)
    {
        return new VertexDiffuse() { Pixel = source };
    }

    public string ToString(string format) => Pixel.ToString(format);

    public override string ToString() => Pixel.ToString("X8");
}
