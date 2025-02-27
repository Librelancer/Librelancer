cbuffer NavmapViewProjection : register(b0, UNIFORM_SPACE)
{
    float4x4 ViewProjection;
};

cbuffer NavmapWorld : register(b2, UNIFORM_SPACE)
{
    float4x4 World;
};

float4 main([[vk::location(0)]] float4 position : POSITION) : SV_Position
{
    return mul(position, mul(World, ViewProjection));
}
