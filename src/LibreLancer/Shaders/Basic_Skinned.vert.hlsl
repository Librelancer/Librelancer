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
    [[vk::location(2)]] float3 normal: NORMAL;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
    [[vk::location(10)]] int4 boneIds: TEXCOORD8;
    [[vk::location(11)]] float4 boneWeights: TEXCOORD9;
};

StructuredBuffer<float4x4> Bones : register(t9, TEXTURE_SPACE);

cbuffer MaterialAnim : register(b4, UNIFORM_SPACE)
{
    float4 MaterialAnim;
}



Output main(VSInput input)
{
    Output output;

    float4x4 boneTransform =
       Bones[input.boneIds[0]] * input.boneWeights[0] +
       Bones[input.boneIds[1]] * input.boneWeights[1] +
       Bones[input.boneIds[2]] * input.boneWeights[2] +
       Bones[input.boneIds[3]] * input.boneWeights[3];

    float3 skinnedPos = mul(float4(input.position, 1.0), boneTransform).xyz;
    float3 skinnedNormal = mul(float4(input.normal, 1.0), boneTransform).xyz;

    output.position = mul(float4(skinnedPos, 1.0), mul(World, ViewProjection));
    output.worldPosition = mul(float4(skinnedPos, 1.0), World).xyz;
    output.viewPosition = mul(float4(skinnedPos, 1.0), mul(World, View));

    float3 n = mul(float4(skinnedNormal, 0.0), NormalMatrix).xyz;

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
