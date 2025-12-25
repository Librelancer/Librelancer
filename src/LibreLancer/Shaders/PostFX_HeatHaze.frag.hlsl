// Heat Haze / Distortion Post-Processing Fragment Shader
// Applies animated distortion effect simulating heat waves

Texture2D<float4> SceneColor : register(t0, TEXTURE_SPACE);
SamplerState Sampler_Scene : register(s0, TEXTURE_SPACE);

// Uniforms in constant buffer b3 (fragment shader uniform block)
cbuffer PostFXParams : register(b3, UNIFORM_SPACE)
{
    float intensity;      // 0.0 (off) to 0.05 (strong), default 0.01
    float speed;          // 0.1 (slow) to 5.0 (fast), default 1.0
    float scale;          // 1.0 (fine) to 20.0 (coarse), default 8.0
    float time;           // Accumulated time for animation
    float2 resolution;    // Screen resolution
    float2 padding;       // Padding for 16-byte alignment
};

struct Input
{
    float2 texCoord : TEXCOORD0;
};

// Smooth 2D noise for organic distortion patterns
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // Hermite curve for smooth interpolation
    float2 u = f * f * (3.0 - 2.0 * f);

    // Sample at grid corners
    float a = hash(i + float2(0.0, 0.0));
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    // Bilinear interpolation
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Layered noise for more organic movement (Fractal Brownian Motion)
float fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    // 3 octaves for good balance of detail vs performance
    for (int i = 0; i < 3; i++)
    {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }

    return value;
}

float4 main(Input input) : SV_Target0
{
    float2 uv = input.texCoord;

    // Calculate animated noise for X and Y distortion
    // Use different time offsets for each axis for more natural movement
    float2 noiseCoord = uv * scale;

    float n1 = fbm(noiseCoord + float2(time * speed, 0.0));
    float n2 = fbm(noiseCoord + float2(0.0, time * speed * 0.7) + float2(17.3, 8.9));  // Offset for variation

    // Calculate distortion offset
    float2 edgeDist = min(uv, 1.0 - uv);
    float edgeFade = saturate(min(edgeDist.x, edgeDist.y) * 10.0);
    float2 distortion = float2(n1 - 0.5, n2 - 0.5) * 2.0 * intensity * edgeFade;

    // Apply distortion with edge falloff to avoid sampling outside texture
    float2 distortedUV = uv + distortion;

    // Clamp to valid UV range
    distortedUV = saturate(distortedUV);

    // Sample scene with distorted coordinates
    float4 color = SceneColor.Sample(Sampler_Scene, distortedUV);

    return color;
}
