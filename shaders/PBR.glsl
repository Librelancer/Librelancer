@feature ET_ENABLED
@feature NORMALMAP
@feature FADE_ENABLED
@feature METALMAP
@feature ROUGHMAP

@vertex
@include(includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;

#ifdef NORMALMAP
out mat3 tbn;
in vec2 vertex_texture2;
in vec2 vertex_texture3;
#else
out vec3 out_normal;
#endif
out vec2 out_texcoord;

out vec3 world_position;

uniform mat4x4 World;
uniform mat4x4 NormalMatrix;
uniform vec4 MaterialAnim;

void main()
{
    vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
    gl_Position = pos;
    vec4 wp = (World * vec4(vertex_position,1));
    wp.xyz /= wp.w;
    world_position = wp.xyz;

    vec3 n = (NormalMatrix * vec4(vertex_normal,0)).xyz;
    #ifdef NORMALMAP
    vec4 v_tangent = vec4(vertex_texture2.x, vertex_texture2.y, vertex_texture3.x, vertex_texture3.y);
    vec3 normalW = normalize(vec3(NormalMatrix * vec4(vertex_normal.xyz, 0.0)));
    vec3 tangentW = normalize(vec3(NormalMatrix * vec4(v_tangent.xyz, 0.0)));
    vec3 bitangentW = cross(normalW, tangentW) * v_tangent.w;
    tbn = mat3(tangentW, bitangentW, normalW);
    #else
    out_normal = n;
    #endif
    out_texcoord = vec2(
        (vertex_texture1.x + MaterialAnim.x) * MaterialAnim.z,
                        (vertex_texture1.y + MaterialAnim.y) * MaterialAnim.w
    );
}

@fragment
@include(includes/camera.inc)
// Lighting Data
// To be replaced by clustered shading later
#define MAX_LIGHTS 9

uniform ivec4 LightingParameters;
#define LightingEnabled (LightingParameters.x == 1)
#define LightCount (LightingParameters.y)
#define FogMode (LightingParameters.z)
#define NumberOfTilesX (LightingParameters.w)

#define LightsPos(x) (LightData[(x) * 5])
#define LightsColorRange(x) (LightData[(x) * 5 + 1])
#define LightsAttenuation(x) (LightData[(x) * 5 + 2].xyz)
#define LightsDir(x) (LightData[(x) * 5 + 3].xyz)
#define SpotlightParams(x) (LightData[(x) * 5 + 4].xyz)

uniform vec4 LightData[MAX_LIGHTS * 5];

uniform vec4 AmbientColor;
uniform vec4 FogColor;
uniform vec2 FogRange;

#ifdef FEATURES430
#define MAX_NUM_LIGHTS_PER_TILE 512

struct PointLight {
    vec4 position;
    vec4 colorRange;
    vec4 attenuation;
    vec4 blank;
};

struct VisibleIndex {
    int index;
};

// Shader storage buffer objects
layout(std430, binding = 0) readonly buffer LightBuffer {
    PointLight data[];
} lightBuffer;

layout(std430, binding = 1) readonly buffer VisibleLightIndicesBuffer {
    VisibleIndex data[];
} visibleLightIndicesBuffer;
#endif

//Material code
const float M_PI = 3.141592653589793;
const float c_MinRoughness = 0.04;
in vec2 out_texcoord;
in vec3 world_position;
#ifdef NORMALMAP
in mat3 tbn;
#else
in vec3 out_normal;
#endif
out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;
uniform sampler2D EtSampler;
uniform sampler2D NtSampler;
uniform float Oc;
uniform float Roughness;
uniform float Metallic;

uniform vec2 FadeRange;

#ifdef NORMALMAP
vec3 getNormal()
{
    vec3 n = texture(NtSampler, out_texcoord).xyz;
    n.xy = (n.xy * 2.0 - 1.0);
    n.z = sqrt(1.0 - dot(n.xy, n.xy));
    n = normalize(n);
    return normalize(tbn * n);
}
#else
vec3 getNormal()
{
    return normalize(out_normal);
}
#endif

#ifdef METALMAP
uniform sampler2D MtSampler;
float getMetallic()
{
    return clamp(texture(MtSampler, out_texcoord).r * Metallic, 0.0, 1.0);
}
#else
float getMetallic()
{
    return clamp(Metallic, 0.0, 1.0);
}
#endif

#ifdef ROUGHMAP
uniform sampler2D RtSampler;
float getRoughness()
{
    return clamp(texture(RtSampler, out_texcoord).r * Roughness, c_MinRoughness, 1.0);
}
#else
float getRoughness()
{
    return clamp(Roughness, c_MinRoughness, 1.0);
}
#endif
// Encapsulate the various inputs used by the various functions in the shading equation
// We store values in this struct to simplify the integration of alternative implementations
// of the shading terms, outlined in the Readme.MD Appendix.
struct PBRInfo
{
    float NdotL;                  // cos angle between normal and light direction
    float NdotV;                  // cos angle between normal and view direction
    float NdotH;                  // cos angle between normal and half vector
    float LdotH;                  // cos angle between light direction and half vector
    float VdotH;                  // cos angle between view direction and half vector
    float perceptualRoughness;    // roughness value, as authored by the model creator (input to shader)
    float metalness;              // metallic value at the surface
    vec3 reflectance0;            // full reflectance color (normal incidence angle)
    vec3 reflectance90;           // reflectance color at grazing angle
    float alphaRoughness;         // roughness mapped to a more linear change in the roughness (proposed by [2])
    vec3 diffuseColor;            // color contribution from diffuse lighting
    vec3 specularColor;           // color contribution from specular lighting
};



// Basic Lambertian diffuse
// Implementation from Lambert's Photometria https://archive.org/details/lambertsphotome00lambgoog
// See also [1], Equation 1
vec3 diffuseLambert(PBRInfo pbrInputs)
{
    return pbrInputs.diffuseColor / M_PI;
}

// The following equation models the Fresnel reflectance term of the spec equation (aka F())
// Implementation of fresnel from [4], Equation 15
vec3 specularReflection(PBRInfo pbrInputs)
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

vec4 SRGBtoLinear(vec4 srgbIn)
{
    vec3 bLess = step(vec3(0.04045),srgbIn.xyz);
    vec3 linOut = mix( srgbIn.xyz/vec3(12.92), pow((srgbIn.xyz+vec3(0.055))/vec3(1.055),vec3(2.4)), bLess );
    return vec4(linOut, srgbIn.a);
}

float fquadratic(float x, vec3 params)
{
    return x * x * params.x + x * params.y + params.z;
}

void main()
{
    vec3 norm = getNormal();
    float metallic = getMetallic();
    float perceptualRoughness = getRoughness();
    float alphaRoughness = perceptualRoughness * perceptualRoughness;

    vec4 baseColor = SRGBtoLinear(texture(DtSampler, out_texcoord) * Dc);

    vec3 f0 = vec3(0.04);
    vec3 diffuseColor = baseColor.rgb * (vec3(1.0) - f0);
    diffuseColor *= 1.0 - metallic;
    vec3 specularColor = mix(f0, baseColor.rgb, metallic);

    // Compute reflectance.
    float reflectance = max(max(specularColor.r, specularColor.g), specularColor.b);

    // For typical incident reflectance range (between 4% to 100%) set the grazing reflectance to 100% for typical fresnel effect.
    // For very low reflectance range on highly diffuse objects (below 4%), incrementally reduce grazing reflectance to 0%.
    float reflectance90 = clamp(reflectance * 25.0, 0.0, 1.0);
    vec3 specularEnvironmentR0 = specularColor.rgb;
    vec3 specularEnvironmentR90 = vec3(1.0, 1.0, 1.0) * reflectance90;

    vec3 color = vec3(0.0);
    vec3 v = normalize(CameraPosition - world_position);

    for(int i = 0; i < MAX_LIGHTS; i++)
    {
        if(i >= LightCount) break;
        vec3 surfaceToLight;
        float attenuation;
        //LightsPos[i].w is the type of light
        //0: directional, 1: point, 2: pointattencurve
        if (LightsPos(i).w == 0.0) {
            //directional light: LightsPos[i].xyz is direction
            surfaceToLight = normalize(-LightsPos(i).xyz);
            attenuation = 1.;
        } else {
            //point light
            surfaceToLight = normalize(LightsPos(i).xyz - world_position);
            float distanceToLight = length(LightsPos(i).xyz - world_position);
            vec3 curve = LightsAttenuation(i);
            float atten0 = attenuation = 1.0 / (curve.x + curve.y * distanceToLight + curve.z * (distanceToLight * distanceToLight));
            float atten1 = fquadratic(distanceToLight / max(LightsColorRange(i).w,1.),curve);
            attenuation = mix(atten0, atten1, LightsPos(i).w - 1.0); //choose correct attenuation
            if(SpotlightParams(i).x > 0.0) { //It's a spotlight
                float NdotL = max(dot(norm, LightsDir(i)), 0.0);
                if(NdotL > 0.0) {
                    float rho = dot(surfaceToLight, -LightsDir(i));
                    attenuation *= pow(clamp((rho - SpotlightParams(i).z) / (SpotlightParams(i).y - SpotlightParams(i).z),0.,1.), SpotlightParams(i).x);
                } else {
                    attenuation = 0.0;
                }
            }
        }
        vec3 l = surfaceToLight;
        vec3 h = normalize(l+v);
        float NdotL = clamp(dot(norm, l), 0.001, 1.0);
        float NdotV = clamp(abs(dot(norm, v)), 0.001, 1.0);
        float NdotH = clamp(dot(norm, h), 0.0, 1.0);
        float LdotH = clamp(dot(l, h), 0.0, 1.0);
        float VdotH = clamp(dot(v, h), 0.0, 1.0);
        PBRInfo pbrInputs = PBRInfo(
            NdotL,
            NdotV,
            NdotH,
            LdotH,
            VdotH,
            perceptualRoughness,
            metallic,
            specularEnvironmentR0,
            specularEnvironmentR90,
            alphaRoughness,
            diffuseColor,
            specularColor
        );

        // Calculate the shading terms for the microfacet specular shading model
        vec3 F = specularReflection(pbrInputs);
        float G = geometricOcclusion(pbrInputs);
        float D = microfacetDistribution(pbrInputs);

        // Calculation of analytical lighting contribution
        vec3 diffuseContrib = (1.0 - F) * diffuseLambert(pbrInputs);
        vec3 specContrib = F * G * D / (4.0 * NdotL * NdotV);
        color += NdotL * LightsColorRange(i).xyz * attenuation * (diffuseContrib + specContrib);
    }
    color += AmbientColor.xyz * baseColor.xyz;

    #ifdef ET_ENABLED
    color += SRGBtoLinear(texture(EtSampler, out_texcoord)).xyz;
    #endif

    out_color =  vec4(pow(color,vec3(1.0/2.2)), baseColor.a);
}
