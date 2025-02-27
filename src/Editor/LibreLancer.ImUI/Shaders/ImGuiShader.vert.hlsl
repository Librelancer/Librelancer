cbuffer UniformBlock : register(b2, UNIFORM_SPACE)
{
    float4x4 ViewProjectionMatrix : packoffset(c0);
};

struct VSInput
{
    [[vk::location(0)]] float2 position: POSITION;
    [[vk::location(1)]] float4 color: COLOR;
    [[vk::location(3)]] float2 uv: TEXCOORD0;
};

struct Output
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
    float4 Position : SV_Position;
};

Output main(VSInput input)
{
    Output output;
    output.TexCoord = input.uv;
    output.Color = input.color;
    output.Position = mul(float4(input.position, 0.0, 1.0), ViewProjectionMatrix);
    return output;
}
