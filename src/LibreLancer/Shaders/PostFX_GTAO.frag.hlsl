// GTAO (Ground Truth Ambient Occlusion) Fragment Shader
// Reference: "Practical Real-time Strategies for Accurate Indirect Occlusion" (Jimenez et al.)
// Uses horizon-based visibility integration for high-quality ambient occlusion

// Include camera matrices from cbuffer b1
#include "includes/Camera.hlsl"

// G-Buffer textures (bound by GBuffer.BindForReading)
Texture2D<float4> GBuffer_Position : register(t0, TEXTURE_SPACE);
Texture2D<float4> GBuffer_Normal : register(t1, TEXTURE_SPACE);
// Note: t2 (Albedo), t3 (Material), t4 (Depth) are bound but not used by SSAO

// Samplers
SamplerState Sampler_Point : register(s0, TEXTURE_SPACE);

// SSAO parameters (cbuffer b3, matches SSAOParams struct in C#)
// Note: Camera matrices come from Camera.hlsl (cbuffer b1), not duplicated here
cbuffer SSAOParams : register(b3, UNIFORM_SPACE)
{
    float ao_radius;           // 0-4: World-space sample radius
    float ao_falloff;          // 4-8: Distance falloff exponent
    float ao_intensity;        // 8-12: AO strength multiplier
    float ao_directions_f;     // 12-16: Number of horizon search directions (as float)
    float ao_steps_f;          // 16-20: Steps per direction (as float)
    float _padding1;           // 20-24
    float _padding2;           // 24-28
    float _padding3;           // 28-32
    float4 resolution_packed;  // 32-48: xy=resolution, zw=1/resolution
};

struct Input
{
    float2 texCoord : TEXCOORD0;
};

// Constants
static const float PI = 3.14159265359;
static const float TWO_PI = 6.28318530718;

// Pseudo-random noise for direction jittering
float interleavedGradientNoise(float2 position)
{
    float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
    return frac(magic.z * frac(dot(position, magic.xy)));
}

// Transform world position to view space (for pre-sampled position)
float3 worldToViewSpace(float3 worldPos)
{
    float4 viewPos = mul(View, float4(worldPos, 1.0));
    return viewPos.xyz;
}

// Get view-space position from G-Buffer world position (with sampling)
float3 getViewSpacePosition(float2 uv)
{
    float4 worldPos = GBuffer_Position.Sample(Sampler_Point, uv);

    // Check validity (alpha channel indicates if pixel has geometry)
    if (worldPos.a < 0.5)
        return float3(0, 0, 1e10);  // Far away = no occlusion

    // Transform to view space using Camera.hlsl View matrix
    return worldToViewSpace(worldPos.xyz);
}

// Get view-space normal from G-Buffer
float3 getViewSpaceNormal(float2 uv)
{
    float3 worldNormal = GBuffer_Normal.Sample(Sampler_Point, uv).xyz;

    // Transform normal to view space (use inverse transpose, but for orthonormal view matrix, this is just the 3x3 part)
    float3 viewNormal = mul((float3x3)View, worldNormal);
    return normalize(viewNormal);
}

// GTAO horizon search in one direction
float searchHorizon(float2 uv, float2 direction, float3 viewPos, float3 viewNormal, float radius)
{
    float2 texelSize = resolution_packed.zw;

    // Screen-space step size based on world radius and distance
    float stepScale = radius / max(1.0, -viewPos.z);
    float2 step = direction * stepScale * texelSize * 100.0;  // Scale to screen space

    float maxHorizon = -1.0;  // cos(horizon angle), starts at -1 (horizon below surface)

    int steps = int(ao_steps_f);
    for (int i = 1; i <= steps; i++)
    {
        float2 sampleUV = uv + step * float(i);

        // Clamp to screen bounds
        if (any(sampleUV < 0.0) || any(sampleUV > 1.0))
            break;

        float3 sampleViewPos = getViewSpacePosition(sampleUV);

        // Vector from current position to sample
        float3 diff = sampleViewPos - viewPos;
        float dist = length(diff);

        // Skip if too far or too close
        if (dist < 0.001 || dist > radius)
            continue;

        // Horizon angle: angle between surface and direction to sample
        float3 horizonDir = diff / dist;
        float horizonCos = dot(horizonDir, viewNormal);

        // Distance-based falloff
        float falloff = 1.0 - pow(dist / radius, ao_falloff);
        horizonCos = lerp(-1.0, horizonCos, falloff);

        maxHorizon = max(maxHorizon, horizonCos);
    }

    return maxHorizon;
}

// GTAO visibility integral approximation
// Computes the unoccluded fraction for a given horizon angle
float computeVisibility(float horizonCos)
{
    // Visibility = (1/pi) * integral of max(0, cos(theta) - cos(h)) dtheta
    // Simplified: visibility = 1 - saturate(horizonCos)
    // Better approximation using sinusoidal integral
    float h = acos(saturate(horizonCos));
    return (sin(h) - h * horizonCos) / PI + 0.5;
}

float4 main(Input input) : SV_Target0
{
    float2 uv = input.texCoord;
    float2 resolution = resolution_packed.xy;

    // Sample G-Buffer position (only once - reused below)
    float4 worldPosData = GBuffer_Position.Sample(Sampler_Point, uv);

    // Skip sky/empty pixels (no geometry)
    if (worldPosData.a < 0.5)
    {
        // No occlusion contribution for sky pixels
        return float4(1.0, 1.0, 1.0, 0.0);
    }

    // Get view-space data (reusing already-sampled position)
    float3 viewPos = worldToViewSpace(worldPosData.xyz);
    float3 viewNormal = getViewSpaceNormal(uv);

    // Early out for invalid normals
    if (length(viewNormal) < 0.5)
        return float4(1.0, 1.0, 1.0, 0.0);

    // Jitter starting angle based on screen position
    float jitter = interleavedGradientNoise(uv * resolution) * TWO_PI;

    // Accumulate occlusion from multiple directions
    float totalVisibility = 0.0;
    int directions = int(ao_directions_f);
    float angleStep = TWO_PI / ao_directions_f;

    for (int d = 0; d < directions; d++)
    {
        float angle = float(d) * angleStep + jitter;
        float2 direction = float2(cos(angle), sin(angle));

        // Search in positive and negative directions
        float horizonPos = searchHorizon(uv, direction, viewPos, viewNormal, ao_radius);
        float horizonNeg = searchHorizon(uv, -direction, viewPos, viewNormal, ao_radius);

        // Compute visibility for both directions
        float visPos = computeVisibility(horizonPos);
        float visNeg = computeVisibility(horizonNeg);

        // Average visibility for this direction pair
        totalVisibility += (visPos + visNeg) * 0.5;
    }

    // Average across all directions
    float ao = totalVisibility / ao_directions_f;

    // Apply intensity
    ao = pow(ao, ao_intensity);

    // Clamp to valid range
    ao = saturate(ao);

    // Output AO in RGB, alpha stores geometry mask for blur pass
    return float4(ao, ao, ao, 1.0);
}
