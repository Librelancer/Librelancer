using System.Diagnostics.CodeAnalysis;
using LibreLancer.Graphics;

namespace LibreLancer.Shaders;

public static class AllShaders
{
    private static bool iscompiled = false;

    public static ShaderBundle AsteroidBand = null!;
    public static ShaderBundle Atmosphere = null!;
    public static ShaderBundle Basic_PositionColor = null!;
    public static ShaderBundle Basic_FVF = null!;
    public static ShaderBundle Basic_PositionTexture = null!;
    public static ShaderBundle Basic_Skinned = null!;
    public static ShaderBundle Billboard = null!;
    public static ShaderBundle DetailMap2Dm1Msk2PassMaterial = null!;
    public static ShaderBundle DetailMapMaterial = null!;
    public static ShaderBundle IllumDetailMapMaterial = null!;
    public static ShaderBundle Masked2DetailMapMaterial = null!;
    public static ShaderBundle Navmap = null!;
    public static ShaderBundle NebulaExtPuff = null!;
    public static ShaderBundle NebulaInterior = null!;
    public static ShaderBundle NebulaMaterial = null!;
    public static ShaderBundle Nomad = null!;
    public static ShaderBundle PBR = null!;
    public static ShaderBundle PhysicsDebug = null!;
    public static ShaderBundle Sprite = null!;
    public static ShaderBundle SunRadial = null!;
    public static ShaderBundle SunSpine = null!;
    public static ShaderBundle ZoneVolume = null!;

    // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

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
        DetailMap2Dm1Msk2PassMaterial ??= Compile(context, "DetailMap2Dm1Msk2PassMaterial");
        DetailMapMaterial ??= Compile(context, "DetailMapMaterial");
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
        // ReSharper restore NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract


        FLLog.Debug("Shaders", "Compile complete");
    }

    private static ShaderBundle Compile(RenderContext context, string name)
    {
        FLLog.Debug("Shaders", $"Compiling {name}");
        return ShaderBundle.FromResource<FreelancerGame>(context, $"{name}.bin");
    }
}
