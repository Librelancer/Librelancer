#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float3 worldPosition: TEXCOORD0;
    float3 normal: TEXCOORD1;
    float4 position: SV_POSITION;
};

struct Input
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(2)]] float3 normal: NORMAL;
};

Output main(Input input)
{
    Output output;
    output.worldPosition = mul(float4(input.position, 1.0), World).xyz;
    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));
    output.normal = mul(float4(input.normal, 0.0), NormalMatrix).xyz;
    return output;
}
