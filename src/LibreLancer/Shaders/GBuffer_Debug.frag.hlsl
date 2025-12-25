// G-Buffer Debug Visualization Fragment Shader
// Displays individual G-Buffer channels for debugging purposes
//
// Debug Modes:
//   0 = Position (world XYZ as RGB)
//   1 = Normal (remapped to [0,1])
//   2 = Albedo
//   3 = Metallic
//   4 = Roughness
//   5 = AO
//   6 = Emissive
//   7 = Depth (linearized)
//   8 = Material Combined (RGB = Metallic/Roughness/AO)

#include "includes/Camera.hlsl"

// G-Buffer textures
Texture2D<float4> GBuffer_Position : register(t0, TEXTURE_SPACE);
SamplerState Sampler_Position : register(s0, TEXTURE_SPACE);

Texture2D<float4> GBuffer_Normal : register(t1, TEXTURE_SPACE);
SamplerState Sampler_Normal : register(s1, TEXTURE_SPACE);

Texture2D<float4> GBuffer_Albedo : register(t2, TEXTURE_SPACE);
SamplerState Sampler_Albedo : register(s2, TEXTURE_SPACE);

Texture2D<float4> GBuffer_Material : register(t3, TEXTURE_SPACE);
SamplerState Sampler_Material : register(s3, TEXTURE_SPACE);

Texture2D<float> GBuffer_Depth : register(t4, TEXTURE_SPACE);
SamplerState Sampler_Depth : register(s4, TEXTURE_SPACE);

cbuffer DebugParameters : register(b3, UNIFORM_SPACE)
{
    float4 DebugParams;     // x = mode, y = near, z = far, w = unused
};

struct Input
{
    float2 texCoord : TEXCOORD0;
};

// Linearize depth from [0,1] NDC to view space distance
float LinearizeDepth(float depth, float near, float far)
{
    // Assuming reverse-Z projection
    return near * far / (far - depth * (far - near));
}

float4 main(Input input) : SV_Target0
{
    // Sample G-Buffer
    float4 positionSample = GBuffer_Position.Sample(Sampler_Position, input.texCoord);
    float4 normalSample = GBuffer_Normal.Sample(Sampler_Normal, input.texCoord);
    float4 albedoSample = GBuffer_Albedo.Sample(Sampler_Albedo, input.texCoord);
    float4 materialSample = GBuffer_Material.Sample(Sampler_Material, input.texCoord);
    float depthSample = GBuffer_Depth.Sample(Sampler_Depth, input.texCoord);

    float3 color = float3(0, 0, 0);
    int mode = int(DebugParams.x);

    if (mode == 0)
    {
        // Position - visualize world position
        color = frac(positionSample.xyz / 100.0);
    }
    else if (mode == 1)
    {
        // Normal - remap from [-1,1] to [0,1]
        float3 n = normalize(normalSample.xyz);
        color = n * 0.5 + 0.5;
    }
    else if (mode == 2)
    {
        // Albedo
        color = albedoSample.rgb;
    }
    else if (mode == 3)
    {
        // Metallic
        float metallic = materialSample.r;
        color = float3(metallic, metallic, metallic);
    }
    else if (mode == 4)
    {
        // Roughness
        float roughness = materialSample.g;
        color = float3(roughness, roughness, roughness);
    }
    else if (mode == 5)
    {
        // AO
        float ao = materialSample.b;
        color = float3(ao, ao, ao);
    }
    else if (mode == 6)
    {
        // Emissive
        float emissive = materialSample.a;
        color = float3(emissive, emissive, emissive);
    }
    else if (mode == 7)
    {
        // Depth (linearized)
        float near = max(DebugParams.y, 0.1);
        float far = max(DebugParams.z, 100.0);
        float linearDepth = LinearizeDepth(depthSample, near, far);
        float normalized = saturate(linearDepth / far);
        color = float3(normalized, normalized, normalized);
    }
    else if (mode == 8)
    {
        // Material Combined (R=Metallic, G=Roughness, B=AO)
        color = materialSample.rgb;
    }
    else
    {
        // Checkerboard pattern for invalid mode
        float2 checker = floor(input.texCoord * 20.0);
        float c = fmod(checker.x + checker.y, 2.0);
        color = float3(c, 0, 1.0 - c);
    }

    // Mark empty/sky pixels
    if (positionSample.a == 0.0)
    {
        color = float3(0.1, 0.1, 0.1);
    }

    return float4(color, 1.0);
}
