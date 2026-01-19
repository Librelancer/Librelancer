#include "includes/Lighting.hlsl"

Texture2D<float4> DtTexture : register(t0, TEXTURE_SPACE);
SamplerState DtSampler : register(s0, TEXTURE_SPACE);

Texture2D<float4> EtTexture : register(t1, TEXTURE_SPACE);
SamplerState EtSampler : register(s1, TEXTURE_SPACE);

Texture2D<float4> NtTexture : register(t2, TEXTURE_SPACE);
SamplerState NtSampler : register(s2, TEXTURE_SPACE);

struct Input
{
    float2 texCoord1: TEXCOORD0;
    float2 texCoord2: TEXCOORD1;
    float3 worldPosition: TEXCOORD2;
#ifdef NORMALMAP
    float3x3 tbn: TEXCOORD3;
#else
    float3 normal: TEXCOORD3;
#endif
    float4 color: TEXCOORD4;
    float4 viewPosition: TEXCOORD5;
#ifdef VERTEX_LIGHTING
    float3 diffuseTermFront: TEXCOORD6;
    float3 diffuseTermBack: TEXCOORD7;
    float3 ambientTermFront: TEXCOORD8;
    float3 ambientTermBack: TEXCOORD9;
#endif
    bool frontFacing: SV_IsFrontFace;
};

cbuffer MaterialParameters : register(b3, UNIFORM_SPACE)
{
    float4 Dc;
    float4 Ec;
    float2 FadeRange;
    float Oc;
};

cbuffer TexCoordSelectors : register(b5, UNIFORM_SPACE)
{
    int TexCoordSelectors[3];
};

float2 GetTexCoord(int index, Input input)
{
    return TexCoordSelectors[index] > 0 ? input.texCoord2 : input.texCoord1;
}
#ifdef NORMALMAP
float3 getNormal(Input input)
{
    float3 n = NtTexture.Sample(NtSampler, GetTexCoord(2, input)).xyz;
    n.xy = (n.xy * 2.0 - 1.0);
    n.z = sqrt(1.0 - dot(n.xy, n.xy));
    n = normalize(n);
    return normalize(mul(input.tbn, n));
}
#else
float3 getNormal(Input input)
{
    return normalize(input.normal);
}
#endif

float4 main(Input input) : SV_Target0
{
    float4 dtSampled = DtTexture.Sample(DtSampler, GetTexCoord(0, input));
#ifdef ALPHATEST_ENABLED
    if (dtSampled.a < 0.5)
    {
        discard;
    }
#endif
    float4 ec = Ec;
#ifdef ET_ENABLED
    ec += EtTexture.Sample(EtSampler, GetTexCoord(1, input));
#endif
    float4 ac = float4(1.0, 1.0, 1.0, 1.0);

#ifdef VERTEX_LIGHTING
    float4 color = ApplyVertexLighting(ac, ec, Dc * input.color,
        dtSampled,
        input.viewPosition,
        input.frontFacing ? input.diffuseTermFront : input.diffuseTermBack,
        input.frontFacing ? input.ambientTermFront : input.ambientTermBack);
#else
    float4 color = ApplyPixelLighting(ac, ec, Dc * input.color,
        dtSampled,
        input.worldPosition, input.viewPosition,
        getNormal(input), input.frontFacing);
#endif
    float4 acolor = color * float4(1.0, 1.0, 1.0, Oc);

#ifdef FADE_ENABLED
    float dist = length(input.viewPosition);
    //FadeRange - x: near, y: far
    float fadeFactor = (FadeRange.y - dist) / (FadeRange.y - FadeRange.x);
    fadeFactor = clamp(fadeFactor, 0.0, 1.0);
    return float4(acolor.rgb, acolor.a * fadeFactor);
#else
    return acolor;
#endif
}
