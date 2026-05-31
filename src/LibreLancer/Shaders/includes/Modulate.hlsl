float4 Mod2x(float4 a, float4 b)
{
    return float4(2 * a.rgb * b.rgb, a.a * b.a);
}

float3 Mod2x(float3 a, float3 b)
{
    return 2 * a * b;
}
