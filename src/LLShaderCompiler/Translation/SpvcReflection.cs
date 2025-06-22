using LLShaderCompiler.SPIRVCross;

namespace LLShaderCompiler;

using static SpvcHelpers;

public static class SpvcReflection
{
    static void DescError(spvc_compiler compiler, ShaderStage stage, uint id, uint expected, uint actual)
    {
        throw new ShaderCompilerException(ShaderError.InvalidDescriptorSet, "",0,0,
            $"Expected {stage.ToString().ToLower()} descriptor set {expected} for '{Spvc.compiler_get_name(compiler, id)}', got {actual}.");
    }

    public static GPUProgram ReflectProgram(byte[] vertex, byte[] fragment)
    {
        return new GPUProgram(ReflectShader(vertex, ShaderStage.Vertex), ReflectShader(fragment, ShaderStage.Fragment));
    }

    static unsafe ReflectedShader ReflectShader(byte[] spirv, ShaderStage stage)
    {
        using var ctx = ContextHandle.Create();

        uint textureSet = stage == ShaderStage.Fragment ? 2U : 0U;
        uint uniformSet = stage == ShaderStage.Fragment ? 3U : 1U;

        spvc_parsed_ir ir;
        spvc_compiler compiler;
        spvc_resources resources;
        fixed (byte* code = spirv)
        {
            RC(ctx, Spvc.context_parse_spirv(ctx, (uint*)code, (nuint)(spirv.Length / 4), &ir));
        }

        RC(ctx, Spvc.context_create_compiler(ctx, spvc_backend.None, ir, spvc_capture_mode.TakeOwnership, &compiler));
        RC(ctx, Spvc.compiler_create_shader_resources(compiler, &resources));

        spvc_reflected_resource* reflectedResources;

        nuint numTextureSamplers;
        nuint numSeparateSamplers = 0;
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.SampledImage, &reflectedResources, &numTextureSamplers));

        for (nuint i = 0; i < numTextureSamplers; i++)
        {
            var set = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id, SpvDecoration.SpvDecorationDescriptorSet);
            if (set != textureSet)
            {
               DescError(compiler, stage, reflectedResources[i].id, textureSet, set);
            }
        }
        if (numTextureSamplers == 0)
        {
            RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.SeparateSamplers, &reflectedResources, &numSeparateSamplers));
            numTextureSamplers = numSeparateSamplers;
        }


        nuint numStorageTextures;
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.StorageImage, &reflectedResources, &numStorageTextures));

        for (nuint i = 0; i < numStorageTextures; i++)
        {
            var set = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id, SpvDecoration.SpvDecorationDescriptorSet);
            if (set != textureSet)
            {
                DescError(compiler, stage, reflectedResources[i].id, textureSet, set);
            }
        }

        nuint numSeparateImages;
        RC(ctx,
            Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.SeparateImage, &reflectedResources,
                &numSeparateImages));
        numStorageTextures += (numSeparateImages - numSeparateSamplers);

        nuint numStorageBuffers;
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.StorageBuffer, &reflectedResources, &numStorageBuffers));

        for (nuint i = 0; i < numStorageBuffers; i++)
        {
            var set = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id, SpvDecoration.SpvDecorationDescriptorSet);
            if (set != textureSet)
            {
                DescError(compiler, stage, reflectedResources[i].id, textureSet, set);
            }
        }

        nuint numUniformBuffers;
        RC(ctx, Spvc.resources_get_resource_list_for_type(resources, spvc_resource_type.UniformBuffer, &reflectedResources, &numUniformBuffers));

        for (nuint i = 0; i < numUniformBuffers; i++)
        {
            var set = Spvc.compiler_get_decoration(compiler, reflectedResources[i].id, SpvDecoration.SpvDecorationDescriptorSet);
            if (set != uniformSet)
            {
                DescError(compiler, stage, reflectedResources[i].id, uniformSet, set);
            }
        }

        return new ReflectedShader("main", spirv);
    }
}
