cbuffer GridInverseParameters : register(b0, UNIFORM_SPACE)
{
    float4x4 InverseViewProjection;
};

float3 UnprojectPoint(float x, float y, float z)
{
    float4 unprojectedPoint = mul(float4(x,y,z, 1.0), InverseViewProjection);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

struct Output
{
    float3 nearPoint: TEXCOORD0;
    float3 farPoint: TEXCOORD1;
    float4 position: SV_POSITION;
};

Output main([[vk::location(0)]] float3 position : POSITION)
{
    Output output;
    output.nearPoint = UnprojectPoint(position.x, position.y, 0.0); // unprojecting on the near plane
    output.farPoint = UnprojectPoint(position.x, position.y, 1.0); // unprojecting on the far plane
    output.position = float4(position, 1.0);
    return output;
}
