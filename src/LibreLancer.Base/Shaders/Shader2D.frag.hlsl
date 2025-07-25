Texture2D<float4> Texture : register(t7, TEXTURE_SPACE);
SamplerState Sampler : register(s7, TEXTURE_SPACE);

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

cbuffer Parameters : register(b3, UNIFORM_SPACE)
{
    int Blend : packoffset(c0);
}

float4 main(Input input) : SV_Target0
{
    float4 src = input.TexCoord.x < -999 ? float4(1.0, 1.0, 1.0, 1.0) : Texture.Sample(Sampler, input.TexCoord);
    return input.Color * (Blend ? src : float4(1.0, 1.0, 1.0, src.r));
}
