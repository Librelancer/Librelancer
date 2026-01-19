#include "includes/Lighting.hlsl"
#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float2 texCoord1: TEXCOORD0;
    float2 texCoord2: TEXCOORD1;
    float3 worldPosition: TEXCOORD2;
    float3 normal: TEXCOORD3;
    float4 color: TEXCOORD4;
    float4 viewPosition: TEXCOORD5;
#ifdef VERTEX_LIGHTING
    float3 diffuseTermFront: TEXCOORD6;
    float3 diffuseTermBack: TEXCOORD7;
    float3 ambientTermFront: TEXCOORD8;
    float3 ambientTermBack: TEXCOORD9;
#endif
    float4 position : SV_Position;
};

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(1)]] float4 color: TEXCOORD0;
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

    output.texCoord1 = float2(
        (MaterialAnim.x) * MaterialAnim.z,
        (MaterialAnim.y) * MaterialAnim.w
    );
    output.texCoord2 = output.texCoord1;
    output.color = input.color;
#ifdef VERTEX_LIGHTING
    VertexLightTerms lightTerms = CalculateVertexLighting(output.worldPosition, n);
    output.diffuseTermFront = lightTerms.diffuseTermFront;
    output.diffuseTermBack = lightTerms.diffuseTermBack;
    output.ambientTermFront = lightTerms.ambientTermFront;
    output.ambientTermBack = lightTerms.ambientTermBack;
#endif
    return output;
}
