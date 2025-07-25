namespace LLShaderCompiler;

public class ReflectedShader
{
    public uint NumSamplers;
    public uint NumStorageTextures;
    public uint NumStorageBuffers;
    public uint NumUniformBuffers;
    public string EntryPoint;
    public byte[] Code;

    public ReflectedShader(string entryPoint, byte[] code)
    {
        EntryPoint = entryPoint;
        Code = code;
    }
    public ReflectedShader CloneWithCode(byte[] code)
    {
        var n = (ReflectedShader)MemberwiseClone();
        n.Code = code;
        return n;
    }
}
