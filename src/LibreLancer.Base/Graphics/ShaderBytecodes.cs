using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics;

internal unsafe class ShaderBytecodes
{
    private const uint SIGNATURE = 0x72646873; //"shdr"
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ShaderHeader
    {
        public uint Magic;
        public uint Version;
        public uint SpirvLength;
        public uint DxilLength;
        public uint MslLength;
        public uint GlLength;
    }

    private static ref readonly ShaderHeader VerifyHeader(ReadOnlySpan<byte> shader)
    {
        var header = MemoryMarshal.Cast<byte, ShaderHeader>(shader.Slice(0, sizeof(ShaderHeader)));
        if (header[0].Magic != SIGNATURE)
            throw new FormatException("Byte array is not shader");
        if(header[0].Version != 1)
            throw new FormatException("Expected shader version 1");
        return ref header.GetPinnableReference();
    }

    public static ReadOnlySpan<byte> GetGLSL(ReadOnlySpan<byte> shader)
    {
        ref readonly ShaderHeader header = ref VerifyHeader(shader);
        if (header.GlLength == 0)
            throw new Exception("Shader does not support OpenGL");
        var offset = (int)(header.SpirvLength + header.DxilLength + header.MslLength + sizeof(ShaderHeader));
        return shader.Slice(offset, (int)header.GlLength);
    }

    public static ReadOnlySpan<byte> GetMSL(ReadOnlySpan<byte> shader)
    {
        ref readonly ShaderHeader header = ref VerifyHeader(shader);
        var offset = (int)(header.SpirvLength + header.DxilLength + sizeof(ShaderHeader));
        return shader.Slice(offset, (int)header.MslLength);
    }

    public static ReadOnlySpan<byte> GetDXIL(ReadOnlySpan<byte> shader)
    {
        ref readonly ShaderHeader header = ref VerifyHeader(shader);
        var offset = (int)(header.SpirvLength + sizeof(ShaderHeader));
        return shader.Slice(offset, (int)header.DxilLength);
    }

    public static ReadOnlySpan<byte> GetSPIRV(ReadOnlySpan<byte> shader)
    {
        ref readonly ShaderHeader header = ref VerifyHeader(shader);
        return shader.Slice(sizeof(ShaderHeader), (int)header.SpirvLength);
    }
}
