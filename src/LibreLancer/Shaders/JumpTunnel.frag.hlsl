Texture2D<float4> Texture: register(t0, TEXTURE_SPACE);
SamplerState Sampler: register(s0, TEXTURE_SPACE);

struct Input
{
    float2 texCoord: TEXCOORD0;
    float4 color: TEXCOORD1;
};

float4 main(Input input): SV_Target0
{
    return Texture.Sample(Sampler, input.texCoord) * input.color;
}
