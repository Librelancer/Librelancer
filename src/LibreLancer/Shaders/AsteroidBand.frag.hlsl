#include "includes/Lighting.hlsl"
#include "includes/Camera.hlsl"

Texture2D<float4> Texture : register(t0, TEXTURE_SPACE);
SamplerState Sampler : register(s0, TEXTURE_SPACE);

struct Input
{
    float2 texCoord: TEXCOORD0;
    float3 worldPosition: TEXCOORD1;
    float3 normal: TEXCOORD2;
    float4 color: TEXCOORD3;
    float4 viewPosition: TEXCOORD4;
    bool frontFacing: SV_IsFrontFace;
};

cbuffer BandParameters : register(b3, UNIFORM_SPACE)
{
    float4 ColorShift;
    float TextureAspect;
};

#define FADE_DISTANCE 12000.0

float4 main(Input input) : SV_Target0
{
    float dist = distance(CameraPosition, input.worldPosition);
    float delta = max(FADE_DISTANCE - dist, 0.0);
    float alpha = (FADE_DISTANCE - delta) / FADE_DISTANCE;
    float4 tex = Texture.Sample(Sampler, input.texCoord * float2(TextureAspect, 1.0));
    float4 dc = float4(tex.rgb * ColorShift.rgb, tex.a * alpha);
    // These parameters may not be entirely correct
    return ApplyPixelLighting(
        float4(1.0, 1.0, 1.0, 1.0),
        float4(0.0, 0.0, 0.0, 1.0),
        float4(1.0, 1.0, 1.0, 1.0),
        dc,
        input.worldPosition,
        input.viewPosition,
        input.normal,
        input.frontFacing);
};
