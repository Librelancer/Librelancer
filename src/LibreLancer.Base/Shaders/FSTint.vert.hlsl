float4 main(int vert: SV_VertexID) : SV_Position
{
    float2 vertices[3] = { float2(-1,-1), float2(3,-1), float2(-1, 3) };
    return float4(vertices[vert], 0, 1);
}
