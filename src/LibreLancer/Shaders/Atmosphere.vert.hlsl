#include "includes/Camera.hlsl"
#include "includes/Transforms.hlsl"

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(2)]] float3 normal: NORMAL;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
};

struct Output
{
    float2 texCoord: TEXCOORD0;
    float3 N : TEXCOORD1;
    float3 V : TEXCOORD2;
    float4 position: SV_Position;
    float3 worldPosition: TEXCOORD3;
    float4 viewPosition: TEXCOORD4;
    float3 normal: TEXCOORD5;
#ifdef VERTEX_LIGHTING
    float3 diffuseTermFront: TEXCOORD6;
    float3 diffuseTermBack: TEXCOORD7;
    float3 ambientTermFront: TEXCOORD8;
    float3 ambientTermBack: TEXCOORD9;
#endif
};

Output main(VSInput input)
{
    Output output;

    float4x4 modelView = mul(World, View);
    float3x3 mvNormal = (float3x3)modelView;

    float3 n = mul(float4(input.normal, 0), NormalMatrix).xyz;

    output.N = normalize(mul(input.normal, mvNormal));
    output.V = -(mul(float4(input.position, 1.0), modelView).xyz);
    output.texCoord = input.uv;
    output.position = mul(float4(input.position, 1.0), mul(World, ViewProjection));
    output.worldPosition = mul(float4(input.position, 1.0), World).xyz;
    output.viewPosition = mul(float4(input.position, 1.0), mul(World, View));
    output.normal = n;

#ifdef VERTEX_LIGHTING
    VertexLightTerms lightTerms = CalculateVertexLighting(output.worldPosition, n);
    output.diffuseTermFront = lightTerms.diffuseTermFront;
    output.diffuseTermBack = lightTerms.diffuseTermBack;
    output.ambientTermFront = lightTerms.ambientTermFront;
    output.ambientTermBack = lightTerms.ambientTermBack;
#endif
    return output;
}
