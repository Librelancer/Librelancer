namespace LLShaderCompiler;

public record struct GLInputBinding(int Location, string Identifier);
public record struct GLTextureBinding(string Identifier, int Unit, int Sampler);

public record struct GLUniformBlock(int Location, string Identifier, int SizeBytes, bool Integer);

public record struct GLStorageBuffer(int Location, string Identifier, int MaxElements);

public class GLShader
{
    public List<GLInputBinding> Inputs = new();
    public List<GLTextureBinding> Textures = new();
    public List<GLStorageBuffer> Buffers = new();
    public string VertexSource = null!;
    public string FragmentSource = null!;

    public List<GLUniformBlock> Uniforms = new();
}
