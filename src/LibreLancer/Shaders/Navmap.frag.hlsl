Texture2D<float4> Texture : register(t0, TEXTURE_SPACE);
SamplerState Sampler : register(s0, TEXTURE_SPACE);

cbuffer NavmapParameters: register(b3, UNIFORM_SPACE)
{
    float4 Rectangle;
    float2 Tiling;
};

cbuffer Tint : register (b4, UNIFORM_SPACE)
{
    float4 Dc;
}

float4 main(float4 fragCoord: SV_Position) : SV_Target0
{
    float2 uv = (fragCoord.xy - Rectangle.xy) / Rectangle.zw;
    uv *= Tiling;
    return Texture.Sample(Sampler, uv) * Dc;
}
