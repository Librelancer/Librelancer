#include "includes/Lighting.hlsl"
#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float2 texCoord: TEXCOORD0;
    float3 worldPosition: TEXCOORD1;
#ifdef NORMALMAP
    float3x3 tbn: TEXCOORD2;
#else
    float3 normal: TEXCOORD2;
#endif
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
    [[vk::location(2)]] float3 normal: NORMAL;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
    [[vk::location(4)]] float2 tangent0: TEXCOORD1;
    [[vk::location(5)]] float2 tangent1: TEXCOORD2;
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

    float3 n = mul(float4(input.normal, 0), NormalMatrix).xyz;

#ifdef NORMALMAP
    float4 t = float4(input.tangent0, input.tangent1);
    float3 normalW = normalize(n);
    float3 tangentW = normalize(mul(NormalMatrix, float4(t.xyz, 0.0)).xyz);
    float3 bitangentW = cross(normalW, tangentW) * t.w;
    output.tbn = float3x3(normalW, tangentW, bitangentW);
#else
    output.normal = n;
#endif
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
