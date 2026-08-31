// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Data.GameData;

public static class JumpEffectColor
{
    public static Vector3 FromIni(Vector3 color) =>
        Vector3.Clamp(color / 255f, Vector3.Zero, Vector3.One);
}

internal struct JumpRandom(uint seed)
{
    private uint state = seed == 0 ? 0x6d2b79f5u : seed;

    public float Next()
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0x00ffffff) / 16777216f;
    }

    public float Range(float min, float max) => min + ((max - min) * Next());
}

public sealed class GateTunnel : IdentifiableItem
{
    public bool WriteDepthBuffer;
    public int NumSplineControlPoints;
    public float XRange;
    public float YRange;
    public float ZRange;
    public float MinRadius;
    public float MaxRadius;
    public float FarRadiusFactor;
    public float MinSpeed;
    public float MaxSpeed;
    public float TimeToMaxSpeed;
    public float FadeDistance;
    public float NearAlpha;
    public float FarAlpha;
    public int NumTSteps;
    public int NumSSteps;
    public float MinRotation;
    public float MaxRotation;
    public Vector3 MinColor;
    public Vector3 MaxColor;
    public GateTunnelLayer[] Layers = [];
}

public sealed class GateTunnelLayer
{
    public string? Texture;
    public Vector3 Color = Vector3.One;
    public float NearAlphaFactor = 1;
    public float FarAlphaFactor = 1;
    public float RadiusFactor = 1;
    public float UOffset;
    public float VOffset;
    public float Du;
    public float Dv;
    public float VScale = 1;
}

public readonly record struct JumpGateGlow(ResolvedFx? Effect, string? Hardpoint, float CreateTime);

public sealed class JumpGateEffect : IdentifiableItem
{
    public JumpGateGlow[] Glows = [];
    public float JumpOutTime;
    public float JumpOutTunnelTime;
    public float JumpInTunnelTime;
    public float JumpInTime;
    public float KillTimeBeforeDone;
    public ResolvedFx? JumpTunnelEffect;
    public GateTunnel? Tunnel;
    public Vector3 JumpAmbient;
    public Vector3 JumpBackgroundColor;
}

public sealed class JumpShipEffect
{
    public ResolvedFx? JumpOutEffect;
    public ResolvedFx? JumpInEffect;
}
