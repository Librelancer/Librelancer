#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float4 position: SV_Position;
    float3 normal: TEXCOORD0;
};

struct Input
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(2)]] float3 normal: NORMAL;
};

Output main(Input input)
{
    Output output;
    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));
    float sum = input.normal.x + input.normal.y + input.normal.z;
    output.normal = abs(sum) > 0.001 ? input.normal : float3(1.0, 0.0, 0.0);
    return output;
}
