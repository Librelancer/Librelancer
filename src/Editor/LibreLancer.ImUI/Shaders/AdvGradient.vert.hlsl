cbuffer UniformBlock : register(b2, UNIFORM_SPACE)
{
    float4x4 ViewProjectionMatrix : packoffset(c0);
};

struct VSInput
{
    [[vk::location(0)]] float2 position: POSITION;
    [[vk::location(1)]] float4 color: COLOR;
    [[vk::location(3)]] int2 uv: TEXCOORD0;
    [[vk::location(12)]] float4 color2: TEXCOORD1;
};

struct Output
{
    float T : TEXCOORD0;
    float4 Color1 : TEXCOORD1;
    float4 Color2 : TEXCOORD2;
    int Blend: TEXCOORD3;
    float4 Position : SV_Position;
};

Output main(VSInput input)
{
    Output output;
    output.T = float(input.uv.x);
    output.Blend = input.uv.y;
    output.Color1 = input.color;
    output.Color2 = input.color2;
    output.Position = mul(float4(input.position, 0.0, 1.0), ViewProjectionMatrix);
    return output;
}
