#include "includes/Camera.hlsl"

// UBO: So must be aligned to 16
// 64 byte struct, can draw up to 256 per draw call/binding.
struct Particle
{
    float3 position;
    uint color;
    // XYZ: Normal, W: Rotation
    float3 normal;
    float rotate;
    // X: Left, Y: Top, Z: Right, W: Bottom
    float4 texCoords;
    //
    float2 halfSize; // XY + padding
    float _pad1;
    float _pad2;
};

cbuffer ParticleParameters : register(b3, UNIFORM_SPACE)
{
    // 0 = basic
    // 1 = rect
    // 2 = perp
    int Type;
};

// Technically an SSBO, but we use UBOs to emulate this for GL3.0
StructuredBuffer<Particle> Particles : register(t9, TEXTURE_SPACE);

struct Output
{
    float2 texCoord : TEXCOORD0;
    float4 color : TEXCOORD1;
    float4 position : SV_Position;
};

float4 DiffuseToFloat4(uint inCol)
{
    float a = ((inCol & 0xff000000) >> 24);
    float b = ((inCol & 0xff0000) >> 16);
    float g = ((inCol & 0xff00) >> 8);
    float r = ((inCol & 0xff));
    return float4(r,g,b,a)/255.0;
}


Output main(int vertexID: SV_VertexID)
{
    const int index = vertexID / 6;
    const Particle particle = Particles[index];
    const int indices[6] = {0, 1, 2, 1, 3, 2};
    int vertex = indices[vertexID % 6];


    float3 p = particle.position;
    float4 color = DiffuseToFloat4(particle.color);

    float3 right;
    float3 up;

    if (Type == 0)
    {
        // Basic
        right = float3(
            View[0][0],
            View[1][0],
            View[2][0]
        );
        up = float3(
            View[0][1],
            View[1][1],
            View[2][1]
        );
    }
    else if (Type == 1)
    {
        // Rect
        up = particle.normal;
        float3 toCamera = normalize(CameraPosition - p);
        right = cross(toCamera, up);
    }
    else if (Type == 2)
    {
        // Perp
        right = cross(particle.normal, float3(0.0, 1.0, 0.0));
        up = cross(right, particle.normal);
    }

    float s, c;
    sincos(particle.rotate, s, c);

    float3 vUp = (c * right - s * up) * particle.halfSize.x;
    float3 vRight = (s * right + c * up) * particle.halfSize.y;

    float3 positions[4] = {
        p - vRight - vUp,
        p + vRight - vUp,
        p - vRight + vUp,
        p + vRight + vUp
    };

    float2 uvs[4] = {
        particle.texCoords.xw,
        particle.texCoords.zw,
        particle.texCoords.xy,
        particle.texCoords.zy
    };

    Output output;
    output.position = mul(float4(positions[vertex], 1.0), ViewProjection);
    output.texCoord = uvs[vertex];
    output.color = color;
    return output;
}
