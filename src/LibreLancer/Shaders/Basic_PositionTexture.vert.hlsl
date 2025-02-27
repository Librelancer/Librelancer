#include "includes/Lighting.hlsl"
#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float2 texCoord: TEXCOORD0;
    float3 worldPosition: TEXCOORD1;
    float3 normal: TEXCOORD2;
    float4 color: TEXCOORD3;
    float4 viewPosition: TEXCOORD4;
#ifdef VERTEX_LIGHTING
    float3 diffuseTermFront: TEXCOORD5;
    float3 diffuseTermBack: TEXCOORD6;
#endif
    float4 position : SV_Position;
};

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
};

cbuffer MaterialAnim : register(b4, UNIFORM_SPACE)
{
    float4 MaterialAnim;
}

Output main(VSInput input)
{
    Output output;

    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));
    output.worldPosition = mul(float4(input.position, 1.0), World).xyz;
    output.viewPosition = mul(float4(input.position, 1.0), mul(World, View));

    float3 n = float3(0.0, 1.0, 0.0);

    output.normal = n;

    output.texCoord = float2(
        (input.uv.x + MaterialAnim.x) * MaterialAnim.z,
        (input.uv.y + MaterialAnim.y) * MaterialAnim.w
    );
    output.color = float4(1.0, 1.0, 1.0, 1.0);
#ifdef VERTEX_LIGHTING
    VertexLightTerms lightTerms = CalculateVertexLighting(output.worldPosition, n);
    output.diffuseTermFront = lightTerms.diffuseTermFront;
    output.diffuseTermBack = lightTerms.diffuseTermBack;
#endif
    return output;
}
