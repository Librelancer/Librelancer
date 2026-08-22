#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(1)]] float4 color: COLOR;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
};

struct Output
{
    float2 texCoord: TEXCOORD0;
    float4 color: TEXCOORD1;
    float4 position: SV_Position;
};

cbuffer TunnelParameters : register(b3, UNIFORM_SPACE)
{
    float2 Scroll;
    float Opacity;
    float Padding;
};

Output main(VSInput input)
{
    Output output;
    output.texCoord = input.uv + Scroll;
    output.color = float4(input.color.rgb, input.color.a * Opacity);
    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));
    return output;
}
