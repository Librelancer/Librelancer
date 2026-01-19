#include "includes/Lighting.hlsl"
#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct Output
{
    float2 texCoord1: TEXCOORD0;
    float2 texCoord2: TEXCOORD1;
    float3 worldPosition: TEXCOORD2;
#ifdef NORMALMAP
    float3x3 tbn: TEXCOORD3;
#else
    float3 normal: TEXCOORD3;
#endif
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
#ifdef VERTEX_DIFFUSE
    [[vk::location(1)]] float4 color : COLOR;
#endif
    [[vk::location(2)]] float3 normal: NORMAL;
    [[vk::location(3)]] float2 texCoord1: TEXCOORD0;
#ifdef VERTEX_TEXTURE2
    [[vk::location(4)]] float2 texCoord2: TEXCOORD1;
    [[vk::location(5)]] float2 tangent0: TEXCOORD2;
    [[vk::location(6)]] float2 tangent1: TEXCOORD3;
#else
    [[vk::location(4)]] float2 tangent0: TEXCOORD1;
    [[vk::location(5)]] float2 tangent1: TEXCOORD2;
#endif
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
    output.texCoord1 = float2(
        (input.texCoord1.x + MaterialAnim.x) * MaterialAnim.z,
        (input.texCoord1.y + MaterialAnim.y) * MaterialAnim.w
    );
#ifdef VERTEX_TEXTURE2
    output.texCoord2 = input.texCoord2;
#else
    output.texCoord2 = output.texCoord1;
#endif
#ifdef VERTEX_DIFFUSE
    output.color = input.color;
#else
    output.color = float4(1.0, 1.0, 1.0, 1.0);
#endif
#ifdef VERTEX_LIGHTING
    VertexLightTerms lightTerms = CalculateVertexLighting(output.worldPosition, n);
    output.diffuseTermFront = lightTerms.diffuseTermFront;
    output.diffuseTermBack = lightTerms.diffuseTermBack;
    output.ambientTermFront = lightTerms.ambientTermFront;
    output.ambientTermBack = lightTerms.ambientTermBack;
#endif
    return output;
}
