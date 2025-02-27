using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics;
unsafe class BytecodesBundle
{
    public uint FeatureMask { get; private set; }
    public int ShaderCount => shaderOffsets.Length;

    public uint GetFeatures(int shader) => shaderOffsets[shader].Features;

    public ReadOnlySpan<byte> GetShader(int shader) => data.AsSpan(
        shaderOffsets[shader].Offset - dataOffset,
        shaderOffsets[shader].Length
    );

    private Bundled[] shaderOffsets;
    private byte[] data;
    private int dataOffset;

    private byte[] Data;
    private const ulong SIGNATURE = 0x524448534C4C0008; //\b\0LLSHDR
    private const uint VERSION = 1;
    record struct Bundled(uint Features, int Offset, int Length);
    public static BytecodesBundle FromStream(Stream stream)
    {
        using var decomp = new ZstdSharp.DecompressionStream(stream);
        var br = new BinaryReader(decomp);
        if (br.ReadUInt64() != SIGNATURE)
            throw new FormatException("Not a shader bundle");
        if (br.ReadUInt32() != VERSION)
            throw new FormatException($"Expected bundle version {VERSION}");
        var sb = new BytecodesBundle();
        sb.FeatureMask = br.ReadUInt32();
        var bundled = new Bundled[br.ReadInt32()];
        int totalLength = 0;
        for (int i = 0; i < bundled.Length; i++)
        {
            bundled[i] = new(br.ReadUInt32(), br.ReadInt32(), br.ReadInt32());
            totalLength += bundled[i].Length;
        }

        sb.dataOffset = 20 + (bundled.Length * 12);
        sb.data = br.ReadBytes(totalLength);
        sb.shaderOffsets = bundled;
        return sb;
    }
}
