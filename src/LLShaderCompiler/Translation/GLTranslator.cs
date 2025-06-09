using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using LLShaderCompiler.SPIRVCross;
using static LLShaderCompiler.SpvcHelpers;

namespace LLShaderCompiler;

public static class GLTranslator
{
    public static GLShader TranslateProgram(string shName, byte[] vertexSpirv, byte[] fragmentSpirv)
    {
        var shader = new GLShader();
        var (vertexSource, vertexVarying) = ToGLSL(shName, vertexSpirv, shader, ShaderStage.Vertex);
        var (fragmentSource, fragmentVarying) = ToGLSL(shName, fragmentSpirv, shader, ShaderStage.Fragment);
        foreach (var vDest in fragmentVarying)
        {
            var vSrc = vertexVarying.FirstOrDefault(x => x.Name == vDest.Name,
                new("", "", ""));
            if (string.IsNullOrWhiteSpace(vSrc.Name))
            {
                throw new ShaderCompilerException(ShaderError.VaryingMissing, shName,0,0,
                    $"fragment input {vDest.OriginalName} missing matching vertex output");
            }
            if (vSrc.Type != vDest.Type)
            {
                throw new ShaderCompilerException(ShaderError.VaryingMismatch, shName, 0, 0,
                    $"vertex output {vSrc.OriginalName} type {vSrc.Type} does not match fragment input type {vDest.Type}");
            }
        }
        shader.VertexSource = vertexSource;
        shader.FragmentSource = fragmentSource;
        return shader;
    }

    static unsafe GLUniformBlock FlattenBlock(spvc_context ctx, spvc_compiler compiler, spvc_reflected_resource uniform)
    {
        RC(ctx, Spvc.compiler_flatten_buffer_block(compiler, uniform.id));
        var name = Spvc.compiler_get_name(compiler, uniform.base_type_id).Replace(".", "_");
        nuint sz;
        RC(ctx, Spvc.compiler_get_declared_struct_size(compiler, Spvc.compiler_get_type_handle(compiler, uniform.base_type_id), &sz));
        var blockType = Spvc.type_get_member_type(Spvc.compiler_get_type_handle(compiler, uniform.base_type_id), 0);
        var baseType = Spvc.type_get_basetype(Spvc.compiler_get_type_handle(compiler, blockType));
        var binding = Spvc.compiler_get_decoration(compiler, uniform.id, SpvDecoration.SpvDecorationBinding);
        return new((int)binding, name, Align16((int)sz), !(baseType == spvc_basetype.Fp16 || baseType == spvc_basetype.Fp32 || baseType == spvc_basetype.Fp64));
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum GLType
    {
        Float,
        Float2,
        Float3,
        Float4,
        Int,
        Int2,
        Int3,
        Int4,
        Float2x2,
        Float2x3,
        Float2x4,
        Float3x2,
        Float3x3,
        Float3x4,
        Float4x2,
        Float4x3,
        Float4x4,
        Struct,
    }


    static (int Array, GLType Type) GetGLType(spvc_type type)
    {
        int a = -1;
        var arrdim = Spvc.type_get_num_array_dimensions(type);
        if (arrdim > 0)
            a = (int)Spvc.type_get_array_dimension(type, 0);
        var sz = Spvc.type_get_vector_size(type);
        var cols = Spvc.type_get_columns(type);
        var bt = Spvc.type_get_basetype(type);

        (int Array, GLType Type) Unsupported() => throw new Exception($"Unsupported type in buffer {bt}:{cols}x{sz}");

        switch (bt)
        {
            case spvc_basetype.Boolean when cols == 1:
            case spvc_basetype.Int32 when cols == 1:
            case spvc_basetype.Uint32 when cols == 1:
                return sz switch
                {
                    1 => (a, GLType.Int),
                    2 => (a, GLType.Int2),
                    3 => (a, GLType.Int3),
                    4 => (a, GLType.Int4),
                    _ => Unsupported()
                };
            case spvc_basetype.Struct:
                return (a, GLType.Struct);
            case spvc_basetype.Fp32 when cols == 1:
                return sz switch
                {
                    1 => (a, GLType.Float),
                    2 => (a, GLType.Float2),
                    3 => (a, GLType.Float3),
                    4 => (a, GLType.Float4),
                    _ => Unsupported()
                };
            case spvc_basetype.Fp32 when cols == 2:
                return sz switch
                {
                    2 => (a, GLType.Float2x2),
                    3 => (a, GLType.Float2x3),
                    4 => (a, GLType.Float2x4),
                    _ => Unsupported()
                };
            case spvc_basetype.Fp32 when cols == 3:
                return sz switch
                {
                    2 => (a, GLType.Float3x2),
                    3 => (a, GLType.Float3x3),
                    4 => (a, GLType.Float3x4),
                    _ => Unsupported()
                };
            case spvc_basetype.Fp32 when cols == 4:
                return sz switch
                {
                    2 => (a, GLType.Float4x2),
                    3 => (a, GLType.Float4x3),
                    4 => (a,GLType.Float4x4),
                    _ => Unsupported()
                };
        }
        return Unsupported();
    }

    static int Align4(int value) => (value + 3) & ~0x03;
    static int Align8(int value) => (value + 7) & ~0x07;
    static int Align16(int value) => (value + 15) & ~0x0F;

    static int VerifyStructLayoutStd140(string fileName, spvc_compiler compiler, spvc_type_id structTypeId)

    {
        int sz = 0;
        var structType = Spvc.compiler_get_type_handle(compiler, structTypeId);
        var structName = Spvc.compiler_get_name(compiler, structTypeId);
        var members = Spvc.type_get_num_member_types(structType);
        for (uint i = 0; i < members; i++)
        {
            var memberTypeId = Spvc.type_get_member_type(structType, i);
            var memberType = Spvc.compiler_get_type_handle(compiler, memberTypeId);
            var (array, gltype) = GetGLType(memberType);

            // TODO: Figure out what the correct matched packing is for this between std140/std430
            switch (gltype)
            {
                case GLType.Float2x2:
                case GLType.Float2x3:
                case GLType.Float2x4:
                case GLType.Float3x2:
                case GLType.Float3x3:
                case GLType.Float3x4:
                case GLType.Float4x2:
                case GLType.Float4x3:
                    throw new ShaderCompilerException(ShaderError.GLBufferInvalidType, fileName,0,0,
                        "Matrices other than 4x4 not yet supported in GL buffers");
            }
            // Array checks
            if (array == 0)
                throw new ShaderCompilerException(ShaderError.GLBufferInvalidType, fileName,0,0,"Dynamic length arrays are not supported in GL 3.1");
            if (array >= 0 && (gltype != GLType.Float4 && gltype != GLType.Int4 && gltype != GLType.Float4x4))
                throw new ShaderCompilerException(ShaderError.GLBufferInvalidType, fileName,0,0,
                    $"Array of type {gltype} cannot be represented in std140 layout");
            int elemCount = array <= 0 ? 1 : array;
            switch (gltype)
            {
                case GLType.Float:
                case GLType.Int:
                    sz = Align4(sz) + 4;
                    break;
                case GLType.Float2:
                case GLType.Int2:
                    sz = Align8(sz) + 8;
                    break;
                case GLType.Int3:
                case GLType.Float3:
                    sz = Align16(sz) + 12;
                    break;
                case GLType.Int4:
                    sz = Align16(sz) + (16 * elemCount);
                    break;
                case GLType.Float4:
                    sz = Align16(sz) + (16 * elemCount);
                    break;
                case GLType.Struct:
                    if (sz != Align16(sz))
                    {
                        throw new ShaderCompilerException(ShaderError.GLBufferInvalidType, fileName,0,0,
                            $"{structName} struct variables should start on multiple of 16 bytes for std140 compatibility");
                    }
                    sz = Align16(sz) + (VerifyStructLayoutStd140(fileName, compiler, memberTypeId) * elemCount);
                    break;
            }
        }

        if (sz % 16 != 0)
        {
            throw new ShaderCompilerException(ShaderError.GLBufferInvalidType, fileName,0,0,
                $"{structName} size needs to be a multiple of 16 bytes to match std140 rules. Consider adding {sz % 16} floats of padding.");
        }
        return sz;
    }

    static string ReplaceFirst(string str, int searchIdx, string term, string replace)
    {
        int position = str.IndexOf(term, searchIdx, StringComparison.Ordinal);
        if (position < 0)
        {
            return str;
        }
        str = str.Substring(0, position) + replace + str.Substring(position + term.Length);
        return str;
    }

    /*
     * This function takes something in the form of
     * layout (std430) readonly buffer Name {
     *    type _m0[];
     * } instance;
     *
     * And transforms it to
     * layout (std140) uniform Name {
     *  type instance[];
     * };
     *
     * And replaces instance._m0 access with instance
     */
    static bool ProcessStorageBuffer(Dictionary<string, int> bufferSizes, ref string source)
    {
        const string SSBO_DEF = "layout(std430) readonly buffer";

        var idx = source.IndexOf(SSBO_DEF, StringComparison.Ordinal);
        if (idx == -1)
            return false;

        var idxBlockStart = source.IndexOf("{", idx + SSBO_DEF.Length, StringComparison.Ordinal);

        var blockName = source.Substring(idx + SSBO_DEF.Length, idxBlockStart - idx - SSBO_DEF.Length).Trim();

        int braces = 1;
        int idxBlockEnd;
        for (idxBlockEnd = idxBlockStart + 1; idxBlockEnd < source.Length; idxBlockEnd++) {
            if (source[idxBlockEnd] == '}')
            {
                braces--;
            }
            if (braces == 0)
            {
                break;
            }
        }
        idxBlockEnd++;
        var identifierEnd = source.IndexOf(";", idxBlockEnd, StringComparison.Ordinal);
        var identifier = source.Substring(idxBlockEnd, identifierEnd - idxBlockEnd).Trim();

        var sz = bufferSizes[blockName];
        // Remove the generated SPIRV-Cross block instance name as that is invalid GLSL 140.
        // And set a fixed size for the SSBO->UBO translation
        source = ReplaceFirst(source, idxBlockEnd, identifier, "");
        source = ReplaceFirst(source, idxBlockStart, "_m0[]", $"{identifier}[{sz}]");
        source = ReplaceFirst(source, idx, SSBO_DEF, "layout(std140) uniform");
        source = source.Replace($"{identifier}._m0", $"{identifier}");
        return true;
    }

    private const string ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_";
    static string Encode(long number)
    {
        if (number < 0) {
            throw new ArgumentException("number < 0");
        }
        var builder = new StringBuilder();
        var divisor = ALPHABET.Length;
        while (number > 0)
        {
            number = Math.DivRem(number, divisor, out var rem);
            builder.Append(ALPHABET[(int) rem]);
        }
        return builder.ToString();
    }

    record struct Varying(string OriginalName, string Name, string Type);

    private const string FIXUP_STATEMENT = "gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;";
    private const string FIXUP_CONDITIONAL = @"
#if !CLIP_CONTROL_ENABLED
gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;
#endif
";

    static unsafe (string, Varying[]) ToGLSL(string shName, byte[] spirv, GLShader compiled, ShaderStage stage)
    {
        if (stage == ShaderStage.Compute)
        {
            throw new ShaderCompilerException(ShaderError.GLComputeUnsupported, shName,0,0,
                $"GL 3.1 does not support compute '{shName}");
        }

        using var ctx = ContextHandle.Create();

        spvc_parsed_ir ir;
        spvc_compiler compiler;
        spvc_compiler_options options;

        byte* translatedSource;

        fixed (byte* b = spirv)
        {
            RC(ctx, Spvc.context_parse_spirv(ctx, (uint*)b, (nuint)(spirv.Length / 4), &ir));
        }

        RC(ctx ,Spvc.context_create_compiler(ctx, spvc_backend.Glsl, ir, spvc_capture_mode.TakeOwnership, &compiler));

        RC(ctx, Spvc.compiler_create_compiler_options(compiler, &options));
        RC(ctx, Spvc.compiler_options_set_bool(options, spvc_compiler_option.GlslEnable420packExtension, false));
        RC(ctx, Spvc.compiler_options_set_uint(options, spvc_compiler_option.GlslVersion, 140));
        RC(ctx, Spvc.compiler_options_set_bool(options, spvc_compiler_option.FixupDepthConvention, true));

        spvc_resources res;
        RC(ctx,Spvc.compiler_create_shader_resources(compiler, &res));

        //rename interface variables (vertex shader)
        spvc_reflected_resource* varyings;
        nuint numVaryings;
        var varyingType = stage == ShaderStage.Vertex ? spvc_resource_type.StageOutput : spvc_resource_type.StageInput;
        RC(ctx,Spvc.resources_get_resource_list_for_type(res, varyingType, &varyings, &numVaryings));
        var varyingResults = new Varying[numVaryings];
        for (nuint i = 0; i < numVaryings; i++)
        {
            var ogName = Spvc.compiler_get_name(compiler, varyings[i].id);
            var type = GetGLType(Spvc.compiler_get_type_handle(compiler, varyings[i].type_id));
            var typeStr = type.Type + (type.Array > 0 ? $"[{type.Array}]" : "");

            var location = Spvc.compiler_get_decoration(compiler, varyings[i].id, SpvDecoration.SpvDecorationLocation);
            Spvc.compiler_set_name(compiler, varyings[i].id, $"_spvc_varying_{location}");
            varyingResults[i] = new(ogName, $"_spvc_varying_{location}", typeStr);
        }

        if (stage == ShaderStage.Vertex)
        {
            spvc_reflected_resource* inputs;
            nuint numInputs;
            RC(ctx,Spvc.resources_get_resource_list_for_type(res, spvc_resource_type.StageInput, &inputs, &numInputs));
            for (nuint i = 0; i < numInputs; i++)
            {
                var name = Spvc.compiler_get_name(compiler, inputs[i].id).Replace(".", "_");
                if (!Spvc.compiler_has_decoration(compiler, inputs[i].id, SpvDecoration.SpvDecorationLocation))
                {
                    throw new Exception($"{name} does not have binding information");
                }
                var binding = Spvc.compiler_get_decoration(compiler, inputs[i].id, SpvDecoration.SpvDecorationLocation);
                compiled.Inputs.Add(new((int)binding, name));
            }
        }


        // image samplers
        RC(ctx,Spvc.compiler_build_combined_image_samplers(compiler));

        spvc_combined_image_sampler* samplers;
        nuint numSamplers;

        RC(ctx,Spvc.compiler_get_combined_image_samplers(compiler, &samplers, &numSamplers));
        for (nuint i = 0; i < numSamplers; i++)
        {
            var tn = Spvc.compiler_get_name(compiler, samplers[i].image_id);
            var sn = Spvc.compiler_get_name(compiler, samplers[i].sampler_id);
            var samplerName = $"SAMPLER_{tn}_{sn}";
            Spvc.compiler_set_name(compiler, samplers[i].combined_id, samplerName);
            var binding =
                Spvc.compiler_get_decoration(compiler, samplers[i].sampler_id, SpvDecoration.SpvDecorationBinding);
            compiled.Textures.Add(new((int)binding, samplerName));
        }


        spvc_reflected_resource* uniforms;
        nuint numUniforms;
        RC(ctx,Spvc.resources_get_resource_list_for_type(res, spvc_resource_type.UniformBuffer, &uniforms, &numUniforms));
        for (nuint i = 0; i < numUniforms; i++)
        {
           compiled.Uniforms.Add(FlattenBlock(ctx, compiler, uniforms[i]));
        }

        spvc_reflected_resource* buffers;
        nuint numBuffers;
        RC(ctx,Spvc.resources_get_resource_list_for_type(res, spvc_resource_type.StorageBuffer, &buffers, &numBuffers));
        Dictionary<string, int> bufferSizes = new Dictionary<string, int>();
        int remappableIndex = 0;
        for(nuint i = 0; i < numBuffers; i++)
        {
            var name = Marshal.PtrToStringUTF8((IntPtr)buffers[i].name)!;
            var memberName = $"_U_{Encode(((uint)name.GetHashCode()) & 0xFFFFFF)}";
            Spvc.compiler_set_name(compiler, buffers[i].base_type_id, name);
            Spvc.compiler_set_name(compiler, buffers[i].id, memberName);

            var binding = Spvc.compiler_get_decoration(compiler, buffers[i].id, SpvDecoration.SpvDecorationBinding);

            SpvDecoration* decorations;
            nuint numDecorations;
            RC(ctx, Spvc.compiler_get_buffer_block_decorations(compiler, buffers[i].id, &decorations, &numDecorations));
            bool isReadOnly = false;
            for (nuint j = 0; j < numDecorations; j++)
            {
                if (decorations[j] == SpvDecoration.SpvDecorationNonWritable)
                {
                    isReadOnly = true;
                    break;
                }
            }

            if (!isReadOnly)
            {
                throw new ShaderCompilerException(ShaderError.GLBufferNotReadOnly, shName,0,0,
                    $"GL 3.1 does not support writeable buffer '{name}'");
            }

            var tp = Spvc.compiler_get_type_handle(compiler, buffers[i].base_type_id);
            if (Spvc.type_get_basetype(tp) != spvc_basetype.Struct ||
                Spvc.type_get_num_member_types(tp) != 1)
            {
                throw new ShaderCompilerException(ShaderError.GLBufferInvalidType, shName,0,0,
                    $"Unsupported buffer construction (not HLSL?) {name}");
            }

            var bufferElements = Spvc.type_get_member_type(tp, 0);
            var glType = GetGLType(Spvc.compiler_get_type_handle(compiler, bufferElements));

            int bSize = glType.Type switch
            {
                GLType.Float4 => 16,
                GLType.Float4x4 => 64,
                GLType.Struct => VerifyStructLayoutStd140(shName, compiler, bufferElements),
                _  => throw new ShaderCompilerException(ShaderError.GLBufferInvalidType, shName,0,0,
                $"Unsupported buffer type {glType} for {name}. Structured Buffers may be float4x4, float4, or std140 compatible struct.")
            };

            var maxElems = 16384 / bSize;
            bufferSizes[name] = maxElems;
            compiled.Buffers.Add(new((int)binding, name, maxElems));
        }

        RC(ctx,Spvc.compiler_install_compiler_options(compiler, options));
        RC(ctx,Spvc.compiler_compile(compiler, &translatedSource));

        var source = ReplaceFirst(Marshal.PtrToStringUTF8((IntPtr)translatedSource)!, 0, "#version 140\n",
            "").Replace(FIXUP_STATEMENT, FIXUP_CONDITIONAL);

        while (ProcessStorageBuffer(bufferSizes, ref source)) ;

        return (source, varyingResults);
    }
}
