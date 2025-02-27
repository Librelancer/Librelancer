#include "includes/Lighting.hlsl"
#include "includes/Camera.hlsl"

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

#define M_PI 3.141592653589793
#define c_MinRoughness 0.04

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
};

cbuffer PBRParameters : register(b3, UNIFORM_SPACE)
{
    float4 Dc;
    float4 Ec;
    float Oc;
    float Roughness;
    float Metallic;
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
    return clamp(RtTexture.Sample(RtSampler, GetTexCoord(4, input)).r * Roughness, c_MinRoughness, 1.0);
}
#else
float getRoughness(Input input)
{
    return Roughness;
}
#endif

struct PBRInfo
{
    float NdotL;                  // cos angle between normal and light direction
    float NdotV;                  // cos angle between normal and view direction
    float NdotH;                  // cos angle between normal and half vector
    float LdotH;                  // cos angle between light direction and half vector
    float VdotH;                  // cos angle between view direction and half vector
    float perceptualRoughness;    // roughness value, as authored by the model creator (input to shader)
    float metalness;              // metallic value at the surface
    float3 reflectance0;            // full reflectance color (normal incidence angle)
    float3 reflectance90;           // reflectance color at grazing angle
    float alphaRoughness;         // roughness mapped to a more linear change in the roughness (proposed by [2])
    float3 diffuseColor;            // color contribution from diffuse lighting
    float3 specularColor;           // color contribution from specular lighting
};

// Basic Lambertian diffuse
// Implementation from Lambert's Photometria https://archive.org/details/lambertsphotome00lambgoog
// See also [1], Equation 1
float3 diffuseLambert(PBRInfo pbrInputs)
{
    return pbrInputs.diffuseColor / M_PI;
}

// The following equation models the Fresnel reflectance term of the spec equation (aka F())
// Implementation of fresnel from [4], Equation 15
float3 specularReflection(PBRInfo pbrInputs)
{
    return pbrInputs.reflectance0 + (pbrInputs.reflectance90 - pbrInputs.reflectance0) * pow(clamp(1.0 - pbrInputs.VdotH, 0.0, 1.0), 5.0);
}

// This calculates the specular geometric attenuation (aka G()),
// where rougher material will reflect less light back to the viewer.
// This implementation is based on [1] Equation 4, and we adopt their modifications to
// alphaRoughness as input as originally proposed in [2].
float geometricOcclusion(PBRInfo pbrInputs)
{
    float NdotL = pbrInputs.NdotL;
    float NdotV = pbrInputs.NdotV;
    float r = pbrInputs.alphaRoughness;

    float attenuationL = 2.0 * NdotL / (NdotL + sqrt(r * r + (1.0 - r * r) * (NdotL * NdotL)));
    float attenuationV = 2.0 * NdotV / (NdotV + sqrt(r * r + (1.0 - r * r) * (NdotV * NdotV)));
    return attenuationL * attenuationV;
}

// The following equation(s) model the distribution of microfacet normals across the area being drawn (aka D())
// Implementation from ""Average Irregularity Representation of a Roughened Surface for Ray Reflection"" by T. S. Trowbridge, and K. P. Reitz
// Follows the distribution function recommended in the SIGGRAPH 2013 course notes from EPIC Games [1], Equation 3.
float microfacetDistribution(PBRInfo pbrInputs)
{
    float roughnessSq = pbrInputs.alphaRoughness * pbrInputs.alphaRoughness;
    float f = (pbrInputs.NdotH * roughnessSq - pbrInputs.NdotH) * pbrInputs.NdotH + 1.0;
    return roughnessSq / (M_PI * f * f);
}

float4 SRGBtoLinear(float4 srgbIn)
{
    float3 bLess = step((float3)0.04045,srgbIn.xyz);
    float3 linOut = lerp( srgbIn.xyz/(float3)12.92, pow((srgbIn.xyz+(float3)0.055)/(float3)1.055,(float3)2.4), bLess );
    return float4(linOut, srgbIn.a);
}

float fquadratic(float x, float3 params)
{
    return x * x * params.x + x * params.y + params.z;
}

float4 main(Input input) : SV_Target0
{
    float3 n = getNormal(input);
    float metallic = getMetallic(input);
    float perceptualRoughness = getRoughness(input);
    float alphaRoughness = perceptualRoughness * perceptualRoughness;

    float4 baseColor = SRGBtoLinear(DtTexture.Sample(DtSampler, GetTexCoord(0, input)) * Dc);

    float3 f0 = 0.04;
    float3 diffuseColor = baseColor.rgb * ((float3)1.0 - f0);
    diffuseColor *= 1.0 - metallic;
    float3 specularColor = lerp(f0, baseColor.rgb, metallic);

    // Compute reflectance.
    float reflectance = max(max(specularColor.r, specularColor.g), specularColor.b);

    // For typical incident reflectance range (between 4% to 100%) set the grazing reflectance to 100% for typical fresnel effect.
    // For very low reflectance range on highly diffuse objects (below 4%), incrementally reduce grazing reflectance to 0%.
    float reflectance90 = clamp(reflectance * 25.0, 0.0, 1.0);
    float3 specularEnvironmentR0 = specularColor.rgb;
    float3 specularEnvironmentR90 = float3(1.0, 1.0, 1.0) * reflectance90;

    float3 color = 0;
    float3 v = normalize(CameraPosition - input.worldPosition);

    for(int i = 0; i < MAX_LIGHTS; i++)
    {
        if(i >= LightCount) break;
        float3 surfaceToLight;
        float attenuation;
        //LightsPos[i].w is the type of light
        //0: directional, 1: point, 2: pointattencurve
        if (Lights[i].Type == 0)
        {
            surfaceToLight = normalize(-Lights[i].Direction);
            attenuation = 1.0;
        }
        else
        {
            surfaceToLight = normalize(Lights[i].Position - input.worldPosition);
            float distanceToLight = length(surfaceToLight);
            float3 curve = Lights[i].Attenuation;
            attenuation = Lights[i].Type == 1.0
                ? 1.0 / (curve.x + curve.y * distanceToLight + curve.z * (distanceToLight * distanceToLight))
                : quadratic(distanceToLight / max(Lights[i].Range,1.), curve);
            if (Lights[i].Spotlight > 0)
            {
                float rho = dot(surfaceToLight, -Lights[i].Direction);
                float spotlightFactor = pow(clamp((rho - Lights[i].Phi) / (Lights[i].Theta - Lights[i].Phi),0.,1.), Lights[i].Falloff);
                float NdotL = max(dot(n, Lights[i].Direction), 0.0);
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
        float3 l = surfaceToLight;
        float3 h = normalize(l+v);
        float NdotL = clamp(dot(n, l), 0.001, 1.0);
        float NdotV = clamp(abs(dot(n, v)), 0.001, 1.0);
        float NdotH = clamp(dot(n, h), 0.0, 1.0);
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
        // Calculate the shading terms for the microfacet specular shading model
        float3 F = specularReflection(pbrInputs);
        float G = geometricOcclusion(pbrInputs);
        float D = microfacetDistribution(pbrInputs);

        // Calculation of analytical lighting contribution
        float3 diffuseContrib = (1.0 - F) * diffuseLambert(pbrInputs);
        float3 specContrib = F * G * D / (4.0 * NdotL * NdotV);
        color += NdotL * Lights[i].Diffuse * attenuation * (diffuseContrib + specContrib);
    }
    color += AmbientColor.xyz * baseColor.xyz;

    #ifdef ET_ENABLED
    color += SRGBtoLinear(EtTexture.Sample(EtSampler, GetTexCoord(1, input))).xyz;
    #endif

    return  float4(pow(color,(float3)1.0/2.2), baseColor.a);
}
