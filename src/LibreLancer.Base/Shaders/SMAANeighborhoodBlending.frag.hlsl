Texture2D<float4> ColorTex : register(t0, TEXTURE_SPACE);
Texture2D<float4> BlendTex : register(t1, TEXTURE_SPACE);

#define SMAA_INCLUDE_VS 0
#define SMAA_INCLUDE_PS 1
#include "SMAAIntegration.hlsl"

struct VSOutput
{
    float4 svPosition : SV_POSITION;
    float2 texcoord: TEXCOORD0;
    float4 offset: TEXCOORD1;
};

float4 main(VSOutput output) : SV_TARGET
{
    return SMAANeighborhoodBlendingPS(output.texcoord, output.offset, ColorTex, BlendTex);
}
