struct Input
{
    float3 normal: TEXCOORD0;
};

float4 main(Input input) : SV_Target0
{
    float3 n = normalize(input.normal);
    return float4(n * 0.5 + 0.5, 1.0);
}
