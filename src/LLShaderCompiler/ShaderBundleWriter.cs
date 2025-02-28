using System.Text;

namespace LLShaderCompiler;

public record GPUProgram(ReflectedShader Vertex, ReflectedShader Fragment);
public record BundledShader(
    uint Features,
    GPUProgram SPIRV,
    GPUProgram? DXIL,
    GPUProgram MSL,
    GLShader? GL);

public class ShaderBundle
{
    public BundledShader[] Shaders = null!;
}

public class ShaderBundleWriter
{
    private const uint FORMAT_VERSION = 1;

    public static void Write(Stream outStream, ShaderBundle bundle)
    {
        using var comp = new ZstdSharp.CompressionStream(outStream, 22);
        var writer = new BinaryWriter(comp);
        writer.Write("\b\0LLSHDR"u8);
        writer.Write(FORMAT_VERSION);
        uint featuresMask = 0;

        var written = new ShaderBytes[bundle.Shaders.Length];
        for (int i = 0; i < bundle.Shaders.Length; i++)
        {
            var shader = bundle.Shaders[i];
            written[i] = new ShaderBytes()
            {
                SPIRV = GetShaderBytes(shader.SPIRV),
                DXIL = GetShaderBytes(shader.DXIL),
                MSL = GetShaderBytes(shader.MSL),
                GL = shader.GL == null ? null : GetGLBytes(shader.GL)
            };
        }

        int[] offsets = new int[bundle.Shaders.Length];

        int mainOffset = bundle.Shaders.Length * 12 + 20;

        for (int i = 0; i < bundle.Shaders.Length; i++)
        {
            featuresMask |= bundle.Shaders[i].Features;
            offsets[i] = mainOffset;
            mainOffset += written[i].LengthBytes;
        }

        writer.Write(featuresMask);
        writer.Write(bundle.Shaders.Length);
        for (int i = 0; i < bundle.Shaders.Length; i++)
        {
            writer.Write(bundle.Shaders[i].Features);
            writer.Write(offsets[i]);
            writer.Write(written[i].LengthBytes);
        }

        for (int i = 0; i < written.Length; i++)
        {
            written[i].Write(writer);
        }
    }

    static void WriteVarUInt32(BinaryWriter writer, uint u)
    {
        if (u <= 127)
        {
            writer.Write((byte)u);
        }
        else if (u <= 16511)
        {
            u -= 128;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)((u >> 7) & 0x7f));
        }
        else if (u <= 2113662)
        {
            u -= 16512;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 14) & 0x7f));
        }
        else if (u <= 270549118)
        {
            u -= 2113663;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 21) & 0x7f));
        }
        else
        {
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 21) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 28) & 0x7f));
        }
    }

    class ShaderBytes
    {
        public byte[] SPIRV;
        public byte[] DXIL;
        public byte[] MSL;
        public byte[]? GL;

        public int LengthBytes => 24 + SPIRV.Length + DXIL.Length + MSL.Length + (GL?.Length ?? 0);

        public void Write(BinaryWriter writer)
        {
            writer.Write("shdr"u8);
            writer.Write(FORMAT_VERSION);
            writer.Write(SPIRV.Length);
            writer.Write(DXIL.Length);
            writer.Write(MSL.Length);
            writer.Write(GL?.Length ?? 0);
            writer.Write(SPIRV);
            writer.Write(DXIL);
            writer.Write(MSL);
            if (GL != null)
            {
                writer.Write(GL);
            }
        }
    }

    static void WriteUTF8(BinaryWriter writer, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        WriteVarUInt32(writer, (uint)bytes.Length);
        writer.Write(bytes);
    }

    static byte[] GetShaderBytes(GPUProgram? sh)
    {
        if (sh == null)
        {
            return "NULL\0"u8.ToArray();
        }
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        WriteVarUInt32(writer, sh.Vertex.NumSamplers);
        WriteVarUInt32(writer, sh.Vertex.NumStorageTextures);
        WriteVarUInt32(writer, sh.Vertex.NumStorageBuffers);
        WriteVarUInt32(writer, sh.Vertex.NumUniformBuffers);
        WriteUTF8(writer, sh.Vertex.EntryPoint);
        WriteVarUInt32(writer, (uint)sh.Vertex.Code.Length);
        writer.Write(sh.Vertex.Code);
        WriteVarUInt32(writer, sh.Fragment.NumSamplers);
        WriteVarUInt32(writer, sh.Fragment.NumStorageTextures);
        WriteVarUInt32(writer, sh.Fragment.NumStorageBuffers);
        WriteVarUInt32(writer, sh.Fragment.NumUniformBuffers);
        WriteUTF8(writer, sh.Fragment.EntryPoint);
        WriteVarUInt32(writer, (uint)sh.Fragment.Code.Length);
        writer.Write(sh.Vertex.Code);
        return ms.ToArray();
    }

    static byte[] GetGLBytes(GLShader gl)
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write("\0GL\0"u8);
        WriteVarUInt32(writer, (uint)gl.Inputs.Count);
        foreach (var input in gl.Inputs)
        {
            WriteVarUInt32(writer, (uint)input.Location);
            WriteUTF8(writer, input.Identifier);
        }

        WriteVarUInt32(writer, (uint)gl.Textures.Count);
        foreach (var texture in gl.Textures)
        {
            WriteVarUInt32(writer, (uint)texture.Location);
            WriteUTF8(writer, texture.Identifier);
        }

        WriteVarUInt32(writer, (uint)gl.Buffers.Count);
        foreach (var buffer in gl.Buffers)
        {
            WriteVarUInt32(writer, (uint)buffer.Location);
            WriteVarUInt32(writer, (uint)buffer.MaxElements);
            WriteUTF8(writer, buffer.Identifier);
        }

        WriteVarUInt32(writer, (uint)gl.Uniforms.Count);
        foreach (var uniform in gl.Uniforms)
        {
            WriteVarUInt32(writer, (uint)uniform.Location);
            WriteVarUInt32(writer, (uint)uniform.SizeBytes);
            writer.Write(uniform.Integer ? (byte)1 : (byte)0);
            WriteUTF8(writer, uniform.Identifier);
        }

        WriteUTF8(writer, gl.VertexSource);
        WriteUTF8(writer, gl.FragmentSource);
        return ms.ToArray();
    }
}
