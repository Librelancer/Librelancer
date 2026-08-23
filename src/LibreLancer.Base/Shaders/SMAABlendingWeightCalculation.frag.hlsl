Texture2D<float4> EdgesTex : register(t0, TEXTURE_SPACE);
Texture2D<float4> AreaTex : register(t1, TEXTURE_SPACE);
Texture2D<float4> SearchTex : register(t2, TEXTURE_SPACE);

#define SMAA_INCLUDE_VS 0
#define SMAA_INCLUDE_PS 1
#include "SMAAIntegration.hlsl"

struct VSOutput
{
    float4 svPosition : SV_POSITION;
    float2 texcoord: TEXCOORD0;
    float2 pixcoord: TEXCOORD1;
    float4 offset[3]: TEXCOORD2;
};

float4 main(VSOutput input) : SV_TARGET
{
    return SMAABlendingWeightCalculationPS(
        input.texcoord,
        input.pixcoord,
        input.offset, EdgesTex, AreaTex, SearchTex, 0);
}
