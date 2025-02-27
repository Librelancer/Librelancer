using System.Runtime.CompilerServices;
using LLShaderCompiler.SPIRVCross;

namespace LLShaderCompiler;

static class SpvcHelpers
{
    public static void RC(spvc_context context, spvc_result result, [CallerArgumentExpression("result")] string? expr = null)
    {
        expr ??= "spvc";
        switch (result)
        {
            case spvc_result.SPVC_SUCCESS:
                break;
            case spvc_result.SPVC_ERROR_INVALID_SPIRV:
                throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"{expr} failed: Invalid SPIRV. {Spvc.context_get_last_error_string(context)}");
            case spvc_result.SPVC_ERROR_INVALID_ARGUMENT:
                throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"{expr} failed: Invalid argument. {Spvc.context_get_last_error_string(context)}");
            case spvc_result.SPVC_ERROR_OUT_OF_MEMORY:
                throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"{expr} failed: Out of Memory. {Spvc.context_get_last_error_string(context)}");
            case spvc_result.SPVC_ERROR_UNSUPPORTED_SPIRV:
                throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"{expr} failed: Unsupported SPIRV. {Spvc.context_get_last_error_string(context)}");
            default:
                throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"{expr} failed: Unknown Error {(int)result}. {Spvc.context_get_last_error_string(context)}");
        }
    }

    public struct ContextHandle : IDisposable
    {
        public spvc_context Value;

        public static unsafe ContextHandle Create()
        {
            var x = new ContextHandle();
            var result = Spvc.context_create(&x.Value);
            switch (result)
            {
                case spvc_result.SPVC_SUCCESS:
                    return x;
                case spvc_result.SPVC_ERROR_INVALID_SPIRV:
                    throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"spvc_context_create failed: Invalid SPIRV.");
                case spvc_result.SPVC_ERROR_INVALID_ARGUMENT:
                    throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"spvc_context_create failed: Invalid argument.");
                case spvc_result.SPVC_ERROR_OUT_OF_MEMORY:
                    throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"spvc_context_create failed: Out of Memory.");
                case spvc_result.SPVC_ERROR_UNSUPPORTED_SPIRV:
                    throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"spvc_context_create failed: Unsupported SPIRV.");
                default:
                    throw new ShaderCompilerException(ShaderError.SPIRVCrossError, "",0,0,$"spvc_context_create failed: Unknown Error {(int)result}.");
            }
        }
        public void Dispose()
        {
            Spvc.context_destroy(Value);
        }
        public static implicit operator spvc_context(ContextHandle handle) => handle.Value;
    }
}
