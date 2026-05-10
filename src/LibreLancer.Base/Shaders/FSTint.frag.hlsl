cbuffer Parameters : register(b3, UNIFORM_SPACE)
{
    float4 Color;
}

float4 main() : SV_Target0
{
    return Color;
}
