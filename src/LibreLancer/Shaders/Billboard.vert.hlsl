#include "includes/Camera.hlsl"

struct VSInput
{
    [[vk::location(0)]] float3 position: POSITION;
    [[vk::location(1)]] float4 color: COLOR;
    [[vk::location(7)]] float3 dimensions: TEXCOORD7;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
};

struct Output
{
    float2 texCoord : TEXCOORD0;
    float4 innerColor : TEXCOORD1;
    float4 outerColor : TEXCOORD2;
    float4 position : SV_Position;
};

Output main(VSInput input)
{
    float3 srcRight = float3(
        View[0][0],
        View[1][0],
        View[2][0]
    );
    float3 srcUp = float3(
        View[0][1],
        View[1][1],
        View[2][1]
    );
    float2 dim = input.dimensions.xy;
    float s = sin(input.dimensions.z);
    float c = sin(input.dimensions.z);
    float3 up = c * srcRight - s * srcUp;
    float3 right = s * srcRight + c * srcUp;
    float3 pos = input.position + (right * dim.x) + (up * dim.y);

    Output output;
    output.position = mul(float4(pos, 1.0), ViewProjection);
    output.texCoord = input.uv;
    output.innerColor = input.color;
    return output;
}
