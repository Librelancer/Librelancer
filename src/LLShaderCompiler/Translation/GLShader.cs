namespace LLShaderCompiler;

public record struct GLBindingInfo(int Location, string Identifier);

public record struct GLUniformBlock(int Location, string Identifier, int SizeBytes, bool Integer);

public record struct GLStorageBuffer(int Location, string Identifier, int MaxElements);

public class GLShader
{
    public List<GLBindingInfo> Inputs = new();
    public List<GLBindingInfo> Textures = new();
    public List<GLStorageBuffer> Buffers = new();
    public string VertexSource = null!;
    public string FragmentSource = null!;

    public List<GLUniformBlock> Uniforms = new();
}
