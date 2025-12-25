// Fullscreen Triangle Vertex Shader
// Generates a fullscreen triangle from vertex IDs (0, 1, 2) without vertex buffer
// Triangle covers entire screen: (-1,-1), (3,-1), (-1,3)
// This is more efficient than a quad as it avoids the diagonal seam

struct Output
{
    float2 texCoord : TEXCOORD0;
    float4 position : SV_Position;
};

Output main(uint vertexID : SV_VertexID)
{
    Output output;

    // Generate fullscreen triangle vertices from vertex ID
    // Vertex 0: (-1, -1) -> UV (0, 1)
    // Vertex 1: ( 3, -1) -> UV (2, 1)
    // Vertex 2: (-1,  3) -> UV (0, -1)
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
    output.position = float4(uv * 2.0 - 1.0, 0.0, 1.0);

    // Texture coordinates match OpenGL's bottom-left origin
    output.texCoord = uv;

    return output;
}
