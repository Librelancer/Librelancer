using System.Runtime.InteropServices;
using LLShaderCompiler.SPIRVCross;

namespace LLShaderCompiler;
using static SpvcHelpers;

public static class DXILTranslator
{
    public static async Task<GPUProgram> TranslateProgram(GPUProgram source) => new(
        await TranslateShader(source.Vertex, ShaderStage.Vertex),
        await TranslateShader(source.Fragment, ShaderStage.Fragment)
    );

    static (string source, string entry) RunSPIRVCross(ReflectedShader shader, ShaderStage stage)
    {
        using var ctx = ContextHandle.Create();
        spvc_parsed_ir ir;
        spvc_compiler compiler;
        spvc_compiler_options options;
        string translatedSource;
        // We can't await in an unsafe method
        unsafe
        {
            fixed (byte* code = shader.Code)
            {
                RC(ctx, Spvc.context_parse_spirv(ctx, (uint*)code, (nuint)(shader.Code.Length / 4), &ir));
            }
            RC(ctx, Spvc.context_create_compiler(ctx, spvc_backend.Hlsl, ir, spvc_capture_mode.TakeOwnership, &compiler));
            RC(ctx, Spvc.compiler_create_compiler_options(compiler, &options));

            Spvc.compiler_options_set_uint(options, spvc_compiler_option.HlslShaderModel, 60);
            Spvc.compiler_options_set_uint(options, spvc_compiler_option.HlslNonwritableUavTextureAsSrv, 1);
            Spvc.compiler_options_set_uint(options, spvc_compiler_option.HlslFlattenMatrixVertexInputSemantics, 1);
            Spvc.compiler_options_set_bool(options, spvc_compiler_option.HlslUseEntryPointName, true);
            Spvc.compiler_options_set_bool(options, spvc_compiler_option.HlslPointSizeCompat, true);

            RC(ctx,Spvc.compiler_install_compiler_options(compiler, options));
            byte* nativePtr;
            RC(ctx,Spvc.compiler_compile(compiler, &nativePtr));
            translatedSource = Marshal.PtrToStringUTF8((IntPtr)nativePtr)!;
        }

        if (string.IsNullOrWhiteSpace(translatedSource))
        {
            throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "", 0, 0, "SPIRV->DXIL failed");
        }

        SpvExecutionModel model = stage switch
        {
            ShaderStage.Fragment => SpvExecutionModel.SpvExecutionModelFragment,
            ShaderStage.Vertex => SpvExecutionModel.SpvExecutionModelVertex,
            ShaderStage.Compute => SpvExecutionModel.SpvExecutionModelGLCompute,
            _ => throw new NotSupportedException()
        };

        return (translatedSource, Spvc.compiler_get_cleansed_entry_point_name(compiler, shader.EntryPoint, model));
    }

    static async Task<ReflectedShader> TranslateShader(ReflectedShader shader, ShaderStage stage)
    {
        var (translatedSource, entryPointName) = RunSPIRVCross(shader, stage);
        var newCode = await DXC.CompileDXIL(translatedSource, stage);
        var sh = shader.CloneWithCode(newCode);
        sh.EntryPoint = entryPointName;
        return sh;
    }
}
