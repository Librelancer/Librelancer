Texture2D<float4> ColorTex : register(t0, TEXTURE_SPACE);

#define SMAA_INCLUDE_VS 0
#define SMAA_INCLUDE_PS 1
#include "SMAAIntegration.hlsl"

struct VSOutput
{
    float4 svPosition : SV_POSITION;
    float2 texcoord: TEXCOORD0;
    float4 offset[3]: TEXCOORD1;
};

float2 main(VSOutput output) : SV_TARGET
{
#if defined(SMAA_PRESET_B)
    return SMAAColorEdgeDetectionPS(output.texcoord, output.offset, ColorTex);
#else
    return SMAALumaEdgeDetectionPS(output.texcoord, output.offset, ColorTex);
#endif
}
