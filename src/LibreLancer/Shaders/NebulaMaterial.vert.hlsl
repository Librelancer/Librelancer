#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
#ifdef VERTEX_DIFFUSE
    [[vk::location(1)]] float4 color : COLOR;
#endif
    [[vk::location(3)]] float2 texcoord : TEXCOORD0;
};

struct Output
{
    float2 texCoord: TEXCOORD0;
    float4 color: TEXCOORD1;
    float4 position : SV_Position;
};

Output main(VSInput input)
{
    Output output;
#ifdef VERTEX_DIFFUSE
    output.color = input.color;
#else
    output.color = 1;
#endif
    output.texCoord = input.texcoord;
    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));
    return output;
}
