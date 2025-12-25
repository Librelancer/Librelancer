// G-Buffer Fill Fragment Shader for Deferred Rendering
// Outputs geometry and material data to multiple render targets (MRT)
//
// G-Buffer Layout:
//   Target0 (Position):  RGB = World Position, A = unused
//   Target1 (Normal):    RGB = World Normal (normalized), A = unused
//   Target2 (Albedo):    RGB = Diffuse Color, A = Alpha (for alpha test)
//   Target3 (Material):  R = Metallic, G = Roughness, B = AO, A = Emissive intensity

Texture2D<float4> DtTexture : register(t0, TEXTURE_SPACE);
SamplerState DtSampler : register(s0, TEXTURE_SPACE);

Texture2D<float4> EtTexture : register(t1, TEXTURE_SPACE);
SamplerState EtSampler : register(s1, TEXTURE_SPACE);

Texture2D<float4> NtTexture : register(t2, TEXTURE_SPACE);
SamplerState NtSampler : register(s2, TEXTURE_SPACE);

Texture2D<float4> MtTexture : register(t3, TEXTURE_SPACE);
SamplerState MtSampler : register(s3, TEXTURE_SPACE);

Texture2D<float4> RtTexture : register(t4, TEXTURE_SPACE);
SamplerState RtSampler : register(s4, TEXTURE_SPACE);

struct Input
{
    float2 texCoord1: TEXCOORD0;
    float2 texCoord2: TEXCOORD1;
    float3 worldPosition: TEXCOORD2;
#ifdef NORMALMAP
    float3x3 tbn: TEXCOORD3;
#else
    float3 normal: TEXCOORD3;
#endif
    float4 color: TEXCOORD4;
    float4 viewPosition: TEXCOORD5;
    bool frontFacing: SV_IsFrontFace;
};

// G-Buffer output structure for MRT
struct GBufferOutput
{
    float4 position : SV_Target0;   // World position
    float4 normal : SV_Target1;     // World normal
    float4 albedo : SV_Target2;     // Diffuse color + alpha
    float4 material : SV_Target3;   // Metallic, Roughness, AO, Emissive
};

cbuffer MaterialParameters : register(b3, UNIFORM_SPACE)
{
    float4 Dc;          // Diffuse color
    float4 Ec;          // Emissive color
    float Oc;           // Opacity
    float Roughness;    // Material roughness (0 = smooth, 1 = rough)
    float Metallic;     // Material metallic (0 = dielectric, 1 = metal)
    float AO;           // Ambient occlusion factor
};

cbuffer TexCoordSelectors : register(b5, UNIFORM_SPACE)
{
    int TexCoordSelectors[5];
};

float2 GetTexCoord(int index, Input input)
{
    return TexCoordSelectors[index] > 0 ? input.texCoord2 : input.texCoord1;
}

#ifdef NORMALMAP
float3 getNormal(Input input)
{
    float3 n = NtTexture.Sample(NtSampler, GetTexCoord(2, input)).xyz;
    n.xy = (n.xy * 2.0 - 1.0);
    n.z = sqrt(1.0 - dot(n.xy, n.xy));
    n = normalize(n);
    return normalize(mul(input.tbn, n));
}
#else
float3 getNormal(Input input)
{
    return normalize(input.normal);
}
#endif

#ifdef METALMAP
float getMetallic(Input input)
{
    return clamp(MtTexture.Sample(MtSampler, GetTexCoord(3, input)).r * Metallic, 0.0, 1.0);
}
#else
float getMetallic(Input input)
{
    return Metallic;
}
#endif

#ifdef ROUGHMAP
float getRoughness(Input input)
{
    return clamp(RtTexture.Sample(RtSampler, GetTexCoord(4, input)).r * Roughness, 0.04, 1.0);
}
#else
float getRoughness(Input input)
{
    return Roughness;
}
#endif

float getEmissiveIntensity(Input input)
{
    float emissive = 0.0;
#ifdef ET_ENABLED
    float4 etSample = EtTexture.Sample(EtSampler, GetTexCoord(1, input));
    emissive = max(max(etSample.r, etSample.g), etSample.b);
#endif
    // Also consider Ec (emissive color from material)
    float ecIntensity = max(max(Ec.r, Ec.g), Ec.b);
    return max(emissive, ecIntensity);
}

GBufferOutput main(Input input)
{
    GBufferOutput output;

    // Sample diffuse texture
    float4 dtSampled = DtTexture.Sample(DtSampler, GetTexCoord(0, input));

#ifdef ALPHATEST_ENABLED
    // Alpha test - discard transparent fragments
    if (dtSampled.a < 0.5)
    {
        discard;
    }
#endif

    // Get world-space normal (handle front/back facing)
    float3 normal = getNormal(input);
    if (!input.frontFacing)
    {
        normal = -normal;
    }

    // Output 0: World Position
    output.position = float4(input.worldPosition, 1.0);

    // Output 1: World Normal (pack into [-1, 1] range, normalized)
    output.normal = float4(normalize(normal), 0.0);

    // Output 2: Albedo (diffuse color * vertex color * material color)
    float4 albedo = dtSampled * input.color * Dc;
    output.albedo = float4(albedo.rgb, albedo.a * Oc);

    // Output 3: Material properties
    // R = Metallic, G = Roughness, B = AO, A = Emissive intensity
    float metallic = getMetallic(input);
    float roughness = getRoughness(input);
    float ao = AO;  // Could be sampled from a texture in the future
    float emissiveIntensity = getEmissiveIntensity(input);

    output.material = float4(metallic, roughness, ao, emissiveIntensity);

    return output;
}
