#include "includes/Camera.hlsl"

// UBO: So must be aligned to 16
// 64 byte struct, can draw up to 256 per draw call/binding.
struct BeamPoint
{
    float3 position;
    float padding;
    float3 forward;
    float halfSize;
    // X: Left, Y: Top, Z: Right, W: Bottom
    float4 texCoords;
    float4 color;
};

cbuffer ParticleParameters : register(b3, UNIFORM_SPACE)
{
    int RotateNormal;
};

// Technically an SSBO, but we use UBOs to emulate this for GL3.0
StructuredBuffer<BeamPoint> BeamPoints : register(t9, TEXTURE_SPACE);

struct Output
{
    float2 texCoord : TEXCOORD0;
    float4 color : TEXCOORD1;
    float4 position : SV_Position;
};

Output main(int vertexID: SV_VertexID)
{
    const int index = vertexID / 2;
    const BeamPoint particle = BeamPoints[index];
    const int vertex = vertexID % 2;
    const int uvIndex = vertexID % 4;

    float3 p = particle.position;
    float3 fwd = particle.forward;

    float3 toCamera = normalize(CameraPosition - p);
    float3 up = cross(toCamera, fwd);
    float3 n = RotateNormal ? cross(up, fwd) : up;

    float s = particle.halfSize;

    float3 position = vertex == 0
        ? p + n * s
        : p - n * s;


    float2 uvs[4] = {
        particle.texCoords.xw, // tl
        particle.texCoords.zw, // tr
        particle.texCoords.xy, // bl
        particle.texCoords.zy // br
    };

    Output output;
    output.position = mul(float4(position, 1.0), ViewProjection);
    output.texCoord = uvs[uvIndex];
    output.color = particle.color;
    return output;
}
