#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(2)]] float3 normal: NORMAL;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
};

struct Output
{
    float2 texCoord: TEXCOORD0;
    float3 N : TEXCOORD1;
    float3 V : TEXCOORD2;
    float4 position: SV_Position;
};

Output main(VSInput input)
{
    Output output;

    float4x4 modelView = mul(World, View);
    float3x3 mvNormal = (float3x3)modelView;

    output.N = normalize(mul(input.normal, mvNormal));
    output.V = -(mul(float4(input.position, 1.0), modelView).xyz);
    output.texCoord = input.uv;
    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));

    return output;
}
