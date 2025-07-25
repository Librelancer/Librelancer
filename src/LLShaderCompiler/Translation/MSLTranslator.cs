using System.Runtime.InteropServices;
using LLShaderCompiler.SPIRVCross;
using static LLShaderCompiler.SpvcHelpers;

namespace LLShaderCompiler;

public static class MSLTranslator
{
    public static GPUProgram TranslateProgram(GPUProgram source) => new(
        TranslateShader(source.Vertex, ShaderStage.Vertex),
        TranslateShader(source.Fragment, ShaderStage.Fragment)
    );

    static string GetName(spvc_compiler compiler, uint id, uint baseType)
    {
        var n = Spvc.compiler_get_name(compiler, id);
        if (string.IsNullOrWhiteSpace(n))
        {
            return Spvc.compiler_get_name(compiler, baseType)!;
        }
        return n;
    }

    static uint GetDescriptorSetIndex(string shName, spvc_compiler compiler, uint id, uint baseType, string type, uint accept1, uint accept2)
    {
        if (!Spvc.compiler_has_decoration(compiler, id,
                SpvDecoration.SpvDecorationDescriptorSet))
        {
            throw new ShaderCompilerException( ShaderError.MissingDescriptorSet, shName,0,0,
                $"Resource {GetName(compiler, id, baseType)} is missing descriptor set");
        }
        uint descriptorSetIndex = Spvc.compiler_get_decoration(compiler, id, SpvDecoration.SpvDecorationDescriptorSet);
        if(descriptorSetIndex != accept1 &&
           descriptorSetIndex != accept2)
        {
            throw new ShaderCompilerException(ShaderError.InvalidDescriptorSet, shName,0,0,
                $"Descriptor set index for {type} '{GetName(compiler, id, baseType)}' must be {accept1} or {accept2}, got {descriptorSetIndex}.");
        }
        return descriptorSetIndex;
    }

    static unsafe ReflectedShader TranslateShader(ReflectedShader shader, ShaderStage stage)
    {
        using var ctx = ContextHandle.Create();
        spvc_parsed_ir ir;
        spvc_compiler compiler;
        spvc_compiler_options options;
        byte* translatedSource;

        fixed (byte* code = shader.Code)
        {
            RC(ctx, Spvc.context_parse_spirv(ctx, (uint*)code, (nuint)(shader.Code.Length / 4), &ir));
        }

        RC(ctx, Spvc.context_create_compiler(ctx, spvc_backend.Msl, ir, spvc_capture_mode.TakeOwnership, &compiler));

        SpvExecutionModel model = stage switch
        {
            ShaderStage.Fragment => SpvExecutionModel.SpvExecutionModelFragment,
            ShaderStage.Vertex => SpvExecutionModel.SpvExecutionModelVertex,
            ShaderStage.Compute => SpvExecutionModel.SpvExecutionModelGLCompute,
            _ => throw new NotSupportedException()
        };

        //remap
        spvc_resources resources;
        spvc_reflected_resource* reflectedResources;

        nuint numTextureSamplers;
        nuint numStorageTextures;
        nuint numStorageBuffers;
        nuint numUniformBuffers;
        nuint numSeparateSamplers = 0;
        nuint numSeparateImages = 0;

        Span<spvc_msl_resource_binding_2> bufferBindings = stackalloc spvc_msl_resource_binding_2[32];
        Span<spvc_msl_resource_binding_2> textureBindings = stackalloc spvc_msl_resource_binding_2[32];
        int numBufferBindings = 0;
        int numTextureBindings = 0;

        RC(ctx, Spvc.compiler_create_shader_resources(compiler, &resources));

        // Combined texture-samplers
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.SampledImage, &reflectedResources, &numTextureSamplers));

        // HLSL, get separate
        if (numTextureSamplers == 0)
        {
            RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.SeparateSamplers, &reflectedResources, &numSeparateSamplers));
            numTextureSamplers = numSeparateSamplers;
        }

        for (nuint i = 0; i < numTextureSamplers; i++)
        {
            uint descriptorSetIndex = GetDescriptorSetIndex(
                "",
                compiler,
                reflectedResources[i].id, reflectedResources[i].base_type_id,
                "graphics texture-sampler",
                0, 2);
            uint bindingIndex = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id,
                SpvDecoration.SpvDecorationBinding);
            textureBindings[numTextureBindings].stage = model;
            textureBindings[numTextureBindings].desc_set = descriptorSetIndex;
            textureBindings[numTextureBindings].binding = bindingIndex;
            textureBindings[numBufferBindings].count = 1;
            numTextureBindings++;
        }

        // Storage textures
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.StorageImage, &reflectedResources, &numStorageTextures));

        for (nuint i = 0; i < numStorageTextures; i++)
        {
            uint descriptorSetIndex = GetDescriptorSetIndex(
                "",
                compiler,
                reflectedResources[i].id, reflectedResources[i].base_type_id,
                "graphics storage texture",
                0, 2);
            uint bindingIndex = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id,
                SpvDecoration.SpvDecorationBinding);
            textureBindings[numTextureBindings].stage = model;
            textureBindings[numTextureBindings].desc_set = descriptorSetIndex;
            textureBindings[numTextureBindings].binding = bindingIndex;
            textureBindings[numBufferBindings].count = 1;
            numTextureBindings++;
        }

        // If source is HLSL, storage images might be marked as separate images
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.SeparateImage, &reflectedResources, &numSeparateImages));

        for (nuint i = numSeparateSamplers; i < numSeparateImages; i ++)
        {
            uint descriptorSetIndex = GetDescriptorSetIndex(
                "",
                compiler,
                reflectedResources[i].id, reflectedResources[i].base_type_id,
                "graphics storage texture",
                0, 2);
            uint bindingIndex = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id,
                SpvDecoration.SpvDecorationBinding);
            textureBindings[numTextureBindings].stage = model;
            textureBindings[numTextureBindings].desc_set = descriptorSetIndex;
            textureBindings[numTextureBindings].binding = bindingIndex;
            textureBindings[numBufferBindings].count = 1;
            numTextureBindings++;
        }

        // Uniform buffers
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.UniformBuffer, &reflectedResources, &numUniformBuffers));

        for (nuint i = 0; i < numUniformBuffers; i++)
        {
            uint descriptorSetIndex = GetDescriptorSetIndex(
                "",
                compiler,
                reflectedResources[i].id, reflectedResources[i].base_type_id,
                "graphics uniform buffer",
                1, 3);
            uint bindingIndex = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id,
                SpvDecoration.SpvDecorationBinding);
            bufferBindings[numBufferBindings].stage = model;
            bufferBindings[numBufferBindings].desc_set = descriptorSetIndex;
            bufferBindings[numBufferBindings].binding = bindingIndex;
            bufferBindings[numBufferBindings].count = 1;
            numBufferBindings++;
        }

        // Storage buffers
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.StorageBuffer, &reflectedResources, &numStorageBuffers));

        for (nuint i = 0; i < numStorageBuffers; i++)
        {
            uint descriptorSetIndex = GetDescriptorSetIndex(
                "",
                compiler,
                reflectedResources[i].id, reflectedResources[i].base_type_id,
                "graphics storage buffer",
                0, 2);
            uint bindingIndex = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id,
                SpvDecoration.SpvDecorationBinding);
            bufferBindings[numBufferBindings].stage = model;
            bufferBindings[numBufferBindings].desc_set = descriptorSetIndex;
            bufferBindings[numBufferBindings].binding = bindingIndex;
            bufferBindings[numBufferBindings].count = 1;
            numBufferBindings++;
        }

        // Textures first
        for (int i = 0; i < numTextureBindings; i++)
        {
            textureBindings[i].msl_texture = textureBindings[i].binding;
            textureBindings[i].msl_sampler = textureBindings[i].binding;
            var b = textureBindings[i];
            RC(ctx, Spvc.compiler_msl_add_resource_binding_2(compiler, &b));
        }

        // Calculate number of uniform buffers
        int uniformBufferCount = 0;
        for (int i = 0; i < numBufferBindings; i++)
        {
            if (bufferBindings[i].desc_set == 1 ||
                bufferBindings[i].desc_set == 3)
            {
                uniformBufferCount++;
            }
        }

        //Calculate resource indices
        for (int i = 0; i < numBufferBindings; i++)
        {
            if (bufferBindings[i].desc_set == 1 || bufferBindings[i].desc_set == 3)
            {
                // Uniform buffers are alone in the descriptor set
                bufferBindings[i].msl_buffer = bufferBindings[i].binding;
            }
            else
            {
                // Subtract by the texture count because the textures precede the storage buffers in the descriptor set
                bufferBindings[i].msl_buffer =
                    (uint)(uniformBufferCount + (bufferBindings[i].binding - numTextureBindings));
            }
            var b = bufferBindings[i];
            RC(ctx, Spvc.compiler_msl_add_resource_binding_2(compiler, &b));
        }

        // Compile to MSL

        RC(ctx, Spvc.compiler_create_compiler_options(compiler, &options));
        RC(ctx, Spvc.compiler_install_compiler_options(compiler, options));
        RC(ctx,Spvc.compiler_compile(compiler, &translatedSource));

        int len = 0;
        while (translatedSource[len] != 0)
            len++;

        var newCode = new byte[len + 1];
        Marshal.Copy((IntPtr)translatedSource, newCode, 0, len);
        return shader.CloneWithCode(newCode);
    }
}
