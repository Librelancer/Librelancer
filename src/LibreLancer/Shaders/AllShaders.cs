using LibreLancer.Graphics;

namespace LibreLancer.Shaders;

public static class AllShaders
{
    private static bool iscompiled = false;

    public static ShaderBundle AsteroidBand;
    public static ShaderBundle Atmosphere;
    public static ShaderBundle Basic_PositionColor;
    public static ShaderBundle Basic_FVF;
    public static ShaderBundle Basic_PositionTexture;
    public static ShaderBundle Basic_Skinned;
    public static ShaderBundle Billboard;
    public static ShaderBundle DeferredLighting;
    public static ShaderBundle DepthCopy;
    public static ShaderBundle GBuffer_Debug;
    public static ShaderBundle GBuffer_Fill;
    public static ShaderBundle DetailMap2Dm1Msk2PassMaterial;
    public static ShaderBundle DetailMapMaterial;
    public static ShaderBundle IllumDetailMapMaterial;
    public static ShaderBundle Masked2DetailMapMaterial;
    public static ShaderBundle Navmap;
    public static ShaderBundle NebulaExtPuff;
    public static ShaderBundle NebulaInterior;
    public static ShaderBundle NebulaMaterial;
    public static ShaderBundle Nomad;
    public static ShaderBundle PBR;
    public static ShaderBundle PhysicsDebug;
    public static ShaderBundle Sprite;
    public static ShaderBundle SunRadial;
    public static ShaderBundle SunSpine;
    public static ShaderBundle ZoneVolume;
    public static ShaderBundle PostFX_VignetteGrain;
    public static ShaderBundle PostFX_HeatHaze;
    public static ShaderBundle PostFX_GTAO;
    public static ShaderBundle PostFX_GTAOBlur;

    public static void CompileBillboard(RenderContext context) =>
        Billboard ??= Compile(context, "Billboard");

    public static void CompilePhysicsDebug(RenderContext context) =>
        PhysicsDebug ??= Compile(context, "PhysicsDebug");

    public static void Compile(RenderContext context)
    {
        if (iscompiled)
        {
            return;
        }

        iscompiled = true;

        FLLog.Debug("Shaders", "Compiling Game shaders");

        AsteroidBand ??= Compile(context, "AsteroidBand");
        Atmosphere ??= Compile(context, "Atmosphere");
        Basic_PositionColor ??= Compile(context, "Basic_PositionColor");
        Basic_FVF ??= Compile(context, "Basic_FVF");
        Basic_PositionTexture ??= Compile(context, "Basic_PositionTexture");
        Basic_Skinned ??= Compile(context, "Basic_Skinned");
        Billboard ??= Compile(context, "Billboard");
        DeferredLighting ??= Compile(context, "DeferredLighting");
        DepthCopy ??= Compile(context, "DepthCopy");
        GBuffer_Debug ??= Compile(context, "GBuffer_Debug");
        DetailMap2Dm1Msk2PassMaterial ??= Compile(context, "DetailMap2Dm1Msk2PassMaterial");
        DetailMapMaterial ??= Compile(context, "DetailMapMaterial");
        GBuffer_Fill ??= Compile(context, "GBuffer_Fill");
        IllumDetailMapMaterial ??= Compile(context, "IllumDetailMapMaterial");
        Masked2DetailMapMaterial ??= Compile(context, "Masked2DetailMapMaterial");
        Navmap ??= Compile(context, "Navmap");
        NebulaExtPuff ??= Compile(context, "NebulaExtPuff");
        NebulaInterior ??= Compile(context, "NebulaInterior");
        NebulaMaterial ??= Compile(context, "NebulaMaterial");
        Nomad ??= Compile(context, "Nomad");
        PBR ??= Compile(context, "PBR");
        PhysicsDebug ??= Compile(context, "PhysicsDebug");
        Sprite ??= Compile(context, "Sprite");
        SunRadial ??= Compile(context, "SunRadial");
        SunSpine ??= Compile(context, "SunSpine");
        ZoneVolume ??= Compile(context, "ZoneVolume");
        PostFX_VignetteGrain ??= Compile(context, "PostFX_VignetteGrain");
        PostFX_HeatHaze ??= Compile(context, "PostFX_HeatHaze");
        PostFX_GTAO ??= Compile(context, "PostFX_GTAO");
        PostFX_GTAOBlur ??= Compile(context, "PostFX_GTAOBlur");

        FLLog.Debug("Shaders", "Compile complete");
    }

    static ShaderBundle Compile(RenderContext context, string name)
    {
        FLLog.Debug("Shaders", $"Compiling {name}");
        return ShaderBundle.FromResource<FreelancerGame>(context, $"{name}.bin");
    }
}
