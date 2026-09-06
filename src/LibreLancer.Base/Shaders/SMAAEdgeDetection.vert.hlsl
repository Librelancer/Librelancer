#define SMAA_INCLUDE_VS 1
#define SMAA_INCLUDE_PS 0
#include "SMAAIntegration.hlsl"

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
};

struct VSOutput
{
    float4 svPosition : SV_POSITION;
    float2 texcoord: TEXCOORD0;
    float4 offset[3]: TEXCOORD1;
};

VSOutput main(VSInput input) {
    VSOutput output;
    output.svPosition = float4(input.position, 1);
    output.texcoord = input.uv;
    SMAAEdgeDetectionVS(output.texcoord, output.offset);
    return output;
}
