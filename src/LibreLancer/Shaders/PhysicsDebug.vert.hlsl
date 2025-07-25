#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float4 color: TEXCOORD0;
    float4 position: SV_Position;
};

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(1)]] float4 color : COLOR;
};

cbuffer PhysicsDebugParam : register(b3, UNIFORM_SPACE)
{
    float4 Dc;
    float Oc;
};

Output main(VSInput input)
{
    Output output;
    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));
    output.color = Oc == 0.0 ? input.color : Dc;
    return output;
}
