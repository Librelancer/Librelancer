Texture2D<float4> Texture : register(t0, TEXTURE_SPACE);
SamplerState Sampler : register(s0, TEXTURE_SPACE);

struct PSInput
{
    float2 texCoord: TEXCOORD0;
};

cbuffer MaterialParameters: register(b2, UNIFORM_SPACE)
{
    float4 Dc;
};

float4 main(PSInput input) : SV_Target0
{
    return Texture.Sample(Sampler, input.texCoord) * Dc;
}
