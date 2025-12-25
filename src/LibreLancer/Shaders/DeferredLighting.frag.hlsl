// Deferred Lighting Fragment Shader
// Reads from G-Buffer and applies PBR lighting calculations
// Outputs HDR color for later tonemapping

#include "includes/Lighting.hlsl"
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

#define M_PI 3.141592653589793
#define c_MinRoughness 0.04

struct Input
{
    float2 texCoord : TEXCOORD0;
};

// PBR functions
struct PBRInfo
{
    float NdotL;
    float NdotV;
    float NdotH;
    float LdotH;
    float VdotH;
    float perceptualRoughness;
    float metalness;
    float3 reflectance0;
    float3 reflectance90;
    float alphaRoughness;
    float3 diffuseColor;
    float3 specularColor;
};

float3 diffuseLambert(PBRInfo pbrInputs)
{
    return pbrInputs.diffuseColor / M_PI;
}

float3 specularReflection(PBRInfo pbrInputs)
{
    return pbrInputs.reflectance0 + (pbrInputs.reflectance90 - pbrInputs.reflectance0) *
           pow(clamp(1.0 - pbrInputs.VdotH, 0.0, 1.0), 5.0);
}

float geometricOcclusion(PBRInfo pbrInputs)
{
    float NdotL = pbrInputs.NdotL;
    float NdotV = pbrInputs.NdotV;
    float r = pbrInputs.alphaRoughness;

    float attenuationL = 2.0 * NdotL / (NdotL + sqrt(r * r + (1.0 - r * r) * (NdotL * NdotL)));
    float attenuationV = 2.0 * NdotV / (NdotV + sqrt(r * r + (1.0 - r * r) * (NdotV * NdotV)));
    return attenuationL * attenuationV;
}

float microfacetDistribution(PBRInfo pbrInputs)
{
    float roughnessSq = pbrInputs.alphaRoughness * pbrInputs.alphaRoughness;
    float f = (pbrInputs.NdotH * roughnessSq - pbrInputs.NdotH) * pbrInputs.NdotH + 1.0;
    return roughnessSq / (M_PI * f * f);
}

float fquadratic(float x, float3 params)
{
    return x * x * params.x + x * params.y + params.z;
}

float4 main(Input input) : SV_Target0
{
    // Sample G-Buffer
    float4 positionSample = GBuffer_Position.Sample(Sampler_Position, input.texCoord);
    float4 normalSample = GBuffer_Normal.Sample(Sampler_Normal, input.texCoord);
    float4 albedoSample = GBuffer_Albedo.Sample(Sampler_Albedo, input.texCoord);
    float4 materialSample = GBuffer_Material.Sample(Sampler_Material, input.texCoord);

    // Early out for sky/background (no geometry written)
    if (positionSample.a == 0.0)
    {
        discard;
    }

    // Unpack G-Buffer data
    float3 worldPosition = positionSample.xyz;
    float3 normal = normalize(normalSample.xyz);
    float3 albedo = albedoSample.rgb;
    float alpha = albedoSample.a;

    // Material properties: R=Metallic, G=Roughness, B=AO, A=Emissive
    float metallic = materialSample.r;
    float perceptualRoughness = max(materialSample.g, c_MinRoughness);
    float ao = materialSample.b;
    float emissiveIntensity = materialSample.a;

    float alphaRoughness = perceptualRoughness * perceptualRoughness;

    // Calculate PBR material properties
    float3 f0 = float3(0.04, 0.04, 0.04);
    float3 diffuseColor = albedo * ((float3)1.0 - f0);
    diffuseColor *= 1.0 - metallic;
    float3 specularColor = lerp(f0, albedo, metallic);

    float reflectance = max(max(specularColor.r, specularColor.g), specularColor.b);
    float reflectance90 = clamp(reflectance * 25.0, 0.0, 1.0);
    float3 specularEnvironmentR0 = specularColor;
    float3 specularEnvironmentR90 = float3(1.0, 1.0, 1.0) * reflectance90;

    // View direction
    float3 v = normalize(CameraPosition - worldPosition);

    // Accumulate lighting
    float3 color = float3(0, 0, 0);

    // Apply ambient lighting with AO
    color += AmbientColor * albedo * ao;

    // Apply point/directional lights
    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (i >= int(LightCount))
            break;

        float3 surfaceToLight;
        float attenuation;

        if (Lights[i].Type == 0)
        {
            // Directional light
            surfaceToLight = normalize(-Lights[i].Direction);
            attenuation = 1.0;
        }
        else
        {
            // Point/spot light
            surfaceToLight = normalize(Lights[i].Position - worldPosition);
            float distanceToLight = length(Lights[i].Position - worldPosition);
            float3 curve = Lights[i].Attenuation;

            attenuation = Lights[i].Type == 1.0
                ? 1.0 / (curve.x + curve.y * distanceToLight + curve.z * (distanceToLight * distanceToLight))
                : fquadratic(distanceToLight / max(Lights[i].Range, 1.0), curve);

            // Spotlight falloff
            if (Lights[i].Spotlight > 0)
            {
                float rho = dot(surfaceToLight, -Lights[i].Direction);
                float spotlightFactor = pow(clamp((rho - Lights[i].Phi) / (Lights[i].Theta - Lights[i].Phi), 0.0, 1.0), Lights[i].Falloff);
                float NdotL = max(dot(normal, Lights[i].Direction), 0.0);
                if (NdotL > 0.0)
                {
                    attenuation *= spotlightFactor;
                }
                else
                {
                    attenuation = 0.0;
                }
            }
        }

        // Skip if light contribution is negligible
        if (attenuation < 0.001)
            continue;

        // PBR lighting calculation
        float3 l = surfaceToLight;
        float3 h = normalize(l + v);

        float NdotL = clamp(dot(normal, l), 0.001, 1.0);
        float NdotV = clamp(abs(dot(normal, v)), 0.001, 1.0);
        float NdotH = clamp(dot(normal, h), 0.0, 1.0);
        float LdotH = clamp(dot(l, h), 0.0, 1.0);
        float VdotH = clamp(dot(v, h), 0.0, 1.0);

        PBRInfo pbrInputs;
        pbrInputs.NdotL = NdotL;
        pbrInputs.NdotV = NdotV;
        pbrInputs.NdotH = NdotH;
        pbrInputs.LdotH = LdotH;
        pbrInputs.VdotH = VdotH;
        pbrInputs.perceptualRoughness = perceptualRoughness;
        pbrInputs.metalness = metallic;
        pbrInputs.reflectance0 = specularEnvironmentR0;
        pbrInputs.reflectance90 = specularEnvironmentR90;
        pbrInputs.alphaRoughness = alphaRoughness;
        pbrInputs.diffuseColor = diffuseColor;
        pbrInputs.specularColor = specularColor;

        // Calculate shading terms
        float3 F = specularReflection(pbrInputs);
        float G = geometricOcclusion(pbrInputs);
        float D = microfacetDistribution(pbrInputs);

        // Combine diffuse and specular
        float3 diffuseContrib = (1.0 - F) * diffuseLambert(pbrInputs);
        float3 specContrib = F * G * D / (4.0 * NdotL * NdotV);

        color += NdotL * Lights[i].Diffuse * attenuation * (diffuseContrib + specContrib);
    }

    // Add emissive contribution
    if (emissiveIntensity > 0.0)
    {
        color += albedo * emissiveIntensity;
    }

    // Apply fog if enabled
    if (FogMode > 0)
    {
        float4 viewPosition = mul(float4(worldPosition, 1.0), View);
        color = ApplyFog(viewPosition, color);
    }

    // Output HDR color (tonemapping applied later)
    return float4(color, 1.0);
}
