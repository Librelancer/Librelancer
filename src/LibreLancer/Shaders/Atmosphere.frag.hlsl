#include "includes/Lighting.hlsl"

Texture2D<float4> DtTexture : register(t0, TEXTURE_SPACE);
SamplerState DtSampler : register(s0, TEXTURE_SPACE);


struct Input
{
    float2 texCoord: TEXCOORD0;
    float3 N : TEXCOORD1;
    float3 V : TEXCOORD2;
    float3 worldPosition: TEXCOORD3;
    float4 viewPosition: TEXCOORD4;
    float3 normal: TEXCOORD5;
#ifdef VERTEX_LIGHTING
    float3 diffuseTermFront: TEXCOORD6;
    float3 diffuseTermBack: TEXCOORD7;
    float3 ambientTermFront: TEXCOORD8;
    float3 ambientTermBack: TEXCOORD9;
#endif
    bool frontFacing : SV_IsFrontFace;
};

cbuffer AtmosphereParameters : register(b3, UNIFORM_SPACE)
{
    float4 Dc;
    float4 Ac;
    float Oc;
    float Fade;
};

float4 main(Input input) : SV_Target0
{
    float facingRatio = clamp(dot(normalize(input.V), normalize(input.N)), 0.0, 1.0);

    float4 tex = DtTexture.Sample(DtSampler, float2(facingRatio, 0.0));
#ifdef VERTEX_LIGHTING
    float4 lit = ApplyVertexLighting(ac, ec, Dc * input.color,
        DtTexture.Sample(DtSampler, input.texCoord),
        input.viewPosition,
        input.frontFacing ? input.diffuseTermFront : input.diffuseTermBack,
        input.frontFacing ? input.ambientTermFront : input.ambientTermBack);
#else
    float4 lit = ApplyPixelLighting(Ac, 0, Dc,
        1,
        input.worldPosition, input.viewPosition,
        input.normal, input.frontFacing);
#endif

    return tex * float4(lit.rgb, Oc);
}
