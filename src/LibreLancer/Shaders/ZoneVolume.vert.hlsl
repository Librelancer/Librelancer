#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float4 position: SV_Position;
    float3 worldPosition: TEXCOORD0;
    float3 normal: TEXCOORD1;
};

struct Input
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(2)]] float3 normal: NORMAL;
};

cbuffer ZoneParameter : register(b3, UNIFORM_SPACE)
{
    float SizeRatio;
}

Output main(Input input)
{
    Output output;
    float3 p = input.position;
    if (SizeRatio != 0.0)
    {
        p = length(input.position.xz) > 1.1
            ? float3(input.position.x / 2., input.position.y, input.position.z / 2.)
            : float3(input.position.x * SizeRatio, input.position.y, input.position.z * SizeRatio);
    }
    output.position = mul(float4(p, 1.0), mul(World, ViewProjection));
    output.worldPosition = mul(float4(p, 1.0), World).xyz;
    output.normal = mul(float4(input.normal, 0.0), NormalMatrix).xyz;

    return output;
}
