#include "includes/Lighting.hlsl"

Texture2D<float4> DtTexture : register(t0, TEXTURE_SPACE);
SamplerState DtSampler : register(s0, TEXTURE_SPACE);

Texture2D<float4> EtTexture : register(t1, TEXTURE_SPACE);
SamplerState EtSampler : register(s1, TEXTURE_SPACE);

Texture2D<float4> NtTexture : register(t2, TEXTURE_SPACE);
SamplerState NtSampler : register(s2, TEXTURE_SPACE);

struct Input
{
    float2 texCoord: TEXCOORD0;
    float3 worldPosition: TEXCOORD1;
#ifdef NORMALMAP
    float3x3 tbn: TEXCOORD2;
#else
    float3 normal: TEXCOORD2;
#endif
    float4 color: TEXCOORD3;
    float4 viewPosition: TEXCOORD4;
#ifdef VERTEX_LIGHTING
    float3 diffuseTermFront: TEXCOORD5;
    float3 diffuseTermBack: TEXCOORD6;
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
#ifdef NORMALMAP
float3 getNormal(Input input)
{
    float3 n = NtTexture.Sample(NtSampler, input.texCoord).xyz;
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
    float4 sampler = DtTexture.Sample(DtSampler, input.texCoord);
#ifdef ALPHATEST_ENABLED
    if (sampler.a < 0.5)
    {
        discard;
    }
#endif
    float4 ec = Ec;
#ifdef ET_ENABLED
    ec += EtTexture.Sample(EtSampler, input.texCoord);
#endif
    float4 ac = float4(1.0, 1.0, 1.0, 1.0);

#ifdef VERTEX_LIGHTING
    float4 color = ApplyVertexLighting(ac, ec, Dc * input.color,
        DtTexture.Sample(DtSampler, input.texCoord),
        input.viewPosition, input.frontFacing ? input.diffuseTermFront : input.diffuseTermBack);
#else
    float4 color = ApplyPixelLighting(ac, ec, Dc * input.color,
        DtTexture.Sample(DtSampler, input.texCoord),
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
