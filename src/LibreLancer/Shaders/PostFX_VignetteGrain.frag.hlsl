// Vignette + Film Grain Post-Processing Fragment Shader
// Applies subtle vignette darkening at corners and animated film grain noise

Texture2D<float4> SceneColor : register(t0, TEXTURE_SPACE);
SamplerState Sampler_Scene : register(s0, TEXTURE_SPACE);

// Uniforms in constant buffer b3 (fragment shader uniform block)
cbuffer PostFXParams : register(b3, UNIFORM_SPACE)
{
    float vignetteIntensity;    // 0.0 (off) to 2.0 (strong), default 1.5
    float vignetteSoftness;     // 0.1 (sharp) to 1.0 (soft), default 0.4
    float grainIntensity;       // 0.0 (off) to 0.2 (strong), default 0.05
    float time;                 // Accumulated time for grain animation
    float2 resolution;          // Screen resolution for noise scaling
    float2 padding;             // Padding for 16-byte alignment
};

struct Input
{
    float2 texCoord : TEXCOORD0;
};

// Quality noise using hash function
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// Animated film grain noise
float filmGrain(float2 uv, float t)
{
    // Add screen-space variation with time animation
    float2 seed = uv * resolution + float2(t * 100.0, t * 57.0);
    return (hash(seed) - 0.5) * 2.0;  // Centered around 0, range [-1, 1]
}

float4 main(Input input) : SV_Target0
{
    float2 uv = input.texCoord;

    // Sample scene color
    float4 color = SceneColor.Sample(Sampler_Scene, uv);

    // === VIGNETTE ===
    // Calculate distance from center (0.5, 0.5) in normalized coordinates
    float2 centeredUV = uv * 2.0 - 1.0;  // Map [0,1] to [-1,1]
    float dist = length(centeredUV);

    // Apply vignette with smooth falloff
    // smoothstep creates a gradual transition from bright center to dark edges
    float vignette = 1.0 - smoothstep(1.0 - vignetteSoftness, 1.0, dist * vignetteIntensity);
    color.rgb *= vignette;

    // === FILM GRAIN ===
    // Generate animated noise
    float grain = filmGrain(uv, time) * grainIntensity;

    // Apply grain additively (subtle noise overlay)
    // Use luminance-based modulation for more natural look on dark vs bright areas
    float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
    float grainModulation = lerp(0.5, 1.0, luminance);  // Less grain on dark areas
    color.rgb += grain * grainModulation;

    // Clamp to valid range
    color.rgb = saturate(color.rgb);

    return color;
}
