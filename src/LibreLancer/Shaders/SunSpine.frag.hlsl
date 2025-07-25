Texture2D<float4> Texture : register(t0, TEXTURE_SPACE);
SamplerState Sampler : register(s0, TEXTURE_SPACE);

struct Input
{
    float2 texCoord : TEXCOORD0;
    float4 innerColor : TEXCOORD1;
    float4 outerColor : TEXCOORD2;
    float4 position : SV_Position;
};

float4 main(Input input) : SV_Target0
{
    float4 texSample = Texture.Sample(Sampler, input.texCoord);
    float dist = distance(float2(0.5, 0.5), input.texCoord) * 2.;
    float4 blendColor = lerp(input.innerColor, input.outerColor, dist);
    return texSample * blendColor;
}
