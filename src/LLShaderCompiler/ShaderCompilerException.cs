using System.Diagnostics.CodeAnalysis;

namespace LLShaderCompiler;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ShaderError : int
{
    FileNotFound = 1,
    DescriptionSyntaxError = 2,
    DescriptionInvalidError = 3,
    HLSLCompileFailure = 4,
    DXILCompileFailure = 5,
    InvalidDescriptorSet = 6,
    MissingDescriptorSet = 7,
    VaryingMissing = 8,
    VaryingMismatch = 9,
    GLBufferNotReadOnly = 1001,
    GLBufferInvalidType = 1002,
    GLComputeUnsupported = 1003,
    SPIRVCrossError = 2004,
    UnknownError = 9001,
}
public class ShaderCompilerException(ShaderError shaderError, string? file, int line, int column, string message) : Exception(message)
{
    public string ToDiagnosticString()
    {
        string nums = "";
        if (line > 0 && column > 0)
        {
            nums = $"{line}:{column}:";
        }
        else if (line > 0)
        {
            nums = $"{line}:";
        }

        string src = "";
        if (!string.IsNullOrEmpty(file))
        {
            src = $"{file}:{nums} ";
        }
        return $"{src}error SH{((int)shaderError):D4}: {Message}";
    }
}
