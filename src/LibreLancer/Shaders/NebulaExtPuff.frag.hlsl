Texture2D<float4> Texture : register(t0, TEXTURE_SPACE);
SamplerState Sampler : register(s0, TEXTURE_SPACE);

struct PSInput
{
    float2 texCoord: TEXCOORD0;
    float4 innerColor : TEXCOORD1;
    float4 outerColor : TEXCOORD2;
};

cbuffer Uniform : register(b3, UNIFORM_SPACE)
{
    float BlendFactor;
};

float4 main(PSInput input) : SV_Target0
{
    float4 sample = Texture.Sample(Sampler, input.texCoord) * input.innerColor;
    return float4(lerp(sample.rgb, input.outerColor.rgb, BlendFactor), sample.a);
}
