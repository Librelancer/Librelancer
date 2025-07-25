Texture2D<float4> DtTexture : register(t0, TEXTURE_SPACE);
SamplerState DtSampler : register(s0, TEXTURE_SPACE);

Texture2D<float4> NtTexture : register(t1, TEXTURE_SPACE);
SamplerState NtSampler : register(s1, TEXTURE_SPACE);

struct Input
{
    float2 texCoord: TEXCOORD0;
    float3 N : TEXCOORD1;
    float3 V : TEXCOORD2;
};

float4 main(Input input) : SV_Target0
{
    float ratio = (dot(normalize(input.V), normalize(input.N)) + 1.0) / 2.0;
    ratio = clamp(ratio, 0.0, 1.0);

    float4 nt = NtTexture.Sample(NtSampler, float2(ratio, 0.0));
    float4 dt = DtTexture.Sample(DtSampler, input.texCoord);

    return float4(dt.rgb + nt.rgb, dt.a * nt.a);
}
