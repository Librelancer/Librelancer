#include "includes/Camera.hlsl"

TextureCube<float4> Texture : register(t0, TEXTURE_SPACE);
SamplerState Sampler : register(s0, TEXTURE_SPACE);

struct Input
{
    float3 worldPosition: TEXCOORD0;
    float3 normal: TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float3 I = normalize(input.worldPosition - CameraPosition);
    float3 R = reflect(I, normalize(input.normal));

    return float4(Texture.Sample(Sampler, R).rgb, 1.0);
}
