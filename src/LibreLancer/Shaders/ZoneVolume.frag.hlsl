struct Input
{
    float3 worldPosition: TEXCOORD0;
    float3 normal: TEXCOORD1;
};

cbuffer ZoneColor : register(b4, UNIFORM_SPACE)
{
    float4 Diffuse: COLOR0;
}

float4 main(Input input) : SV_Target0
{
    float3 norm = normalize(input.normal);
    float3 lightDir = normalize(float3(0.,-150.,0.) - input.worldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    float3 color = (Diffuse.rgb * diff) * 0.3;
    return float4(Diffuse.rgb * 0.7 + color, Diffuse.a);
}
