#define MAX_LIGHTS 9

struct Light
{
    //1x float4
    float3 Position;
    float Type;
    //2x float4
    float3 Diffuse;
    float Range;
    //3x float4
    float3 Ambient;
    float _pad1;
    //4x float4
    float3 Attenuation;
    float _pad2;
    //5x float4
    float3 Direction;
    float _pad3;
    //6x float4
    float Spotlight;
    float Falloff;
    float Theta;
    float Phi;
};

cbuffer Lighting : register(b2, UNIFORM_SPACE)
{
    //[0]
    float2 FogRange;
    float UseLighting;
    float FogMode; //1x float4
    //[1]
    float3 AmbientColor;
    float LightCount; //2x float4
    //[2]
    float3 FogColor;
    float _Padding; //3x float4
    //[3]
    Light Lights[MAX_LIGHTS];
}


float quadratic(float x, float3 params)
{
    return x * x * params.x + x * params.y + params.z;
}

struct VertexLightTerms
{
    float3 diffuseTermFront;
    float3 diffuseTermBack;
    float3 ambientTermFront;
    float3 ambientTermBack;
};

VertexLightTerms CalculateVertexLighting(float3 position, float3 normal)
{
    float3 diffuseFront = float3(0, 0, 0);
    float3 diffuseBack = float3(0, 0, 0);
    float3 ambientFront = float3(0, 0, 0);
    float3 ambientBack = float3(0, 0, 0);

    float3 n = normalize(normal);
    float3 nBack = -n;
    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (i >= int(LightCount))
            break;
        float3 surfaceToLight;
        float attenuationFront;
        float attenuationBack;

        if (Lights[i].Type == 0)
        {
            surfaceToLight = normalize(-Lights[i].Direction);
            attenuationFront = attenuationBack = 1.0;
        }
        else
        {
            surfaceToLight = normalize(Lights[i].Position - position);
            float distanceToLight = length(Lights[i].Position - position);
            float3 curve = Lights[i].Attenuation;
            float atten = Lights[i].Type == 1.0
                ? 1.0 / (curve.x + curve.y * distanceToLight + curve.z * (distanceToLight * distanceToLight))
                : quadratic(distanceToLight / max(Lights[i].Range,1.), curve);
            if (Lights[i].Spotlight > 0)
            {
                float rho = dot(surfaceToLight, -Lights[i].Direction);
                float theta = Lights[i].Theta;
                float phi = Lights[i].Phi;
                float falloff = Lights[i].Falloff;
                float spotAtten = rho - phi;
                spotAtten = spotAtten / (theta - phi);
                spotAtten = pow(spotAtten, falloff);

                bool insideThetaAndPhi = rho <= theta;
                bool insidePhi = rho > phi;
                spotAtten = insidePhi ? spotAtten : 0.0;
                spotAtten = insideThetaAndPhi ? spotAtten : 1.0;
                spotAtten = clamp(spotAtten, 0.0, 1.0);

                atten *= spotAtten;
            }
            attenuationFront = attenuationBack = atten;
        }

        float diffuseCoefficient = max(dot(n, surfaceToLight), 0.0);
        float3 diffuse = diffuseCoefficient * Lights[i].Diffuse;
        diffuseFront += attenuationFront * diffuse;
        ambientFront += attenuationFront * Lights[i].Ambient;

        float diffuseBackCoeff = max(dot(nBack, surfaceToLight), 0.0);
        diffuseBack += diffuseBackCoeff * Lights[i].Diffuse;
        ambientBack += attenuationBack * Lights[i].Ambient;
    }
    VertexLightTerms result;
    result.diffuseTermFront = clamp(diffuseFront, 0.0, 1.0);
    result.diffuseTermBack = clamp(diffuseBack, 0.0, 1.0);
    result.ambientTermFront = clamp(ambientFront, 0.0, 1.0);
    result.ambientTermBack = clamp(ambientBack, 0.0, 1.0);
    return result;
}

#define FOGMODE_LINEAR 3
#define FOGMODE_EXP 1
#define FOGMODE_EXP2 2

float3 ApplyFog(float4 viewPosition, float3 objectColor)
{
    float fogFactor;
    float dist = length(viewPosition);
    if(FogMode == FOGMODE_EXP)
    {
        //FogRange - x: density
        fogFactor = 1.0 / exp(dist * FogRange.x);
    }
    else if (FogMode == FOGMODE_EXP2)
    {
        //FogRange - x: density
        fogFactor = 1.0 / exp((dist * FogRange.x) * (dist * FogRange.x));
    }
    else
    {
        //FogRange - x: near, y: far
        fogFactor = (FogRange.y - dist) / (FogRange.y - FogRange.x);
    }
    fogFactor = clamp(fogFactor, 0.0, 1.0);
    return lerp(FogColor.rgb, objectColor, fogFactor);
}

float4 ApplyVertexLighting(
    float4 ac,float4 ec, float4 dc, float4 tex,
    float4 viewPosition, float3 vertexDiffuseTerm, float3 vertexAmbientTerm)
{
    if (UseLighting <= 0)
        return dc * tex;
    float3 color = ac.rgb * (AmbientColor + vertexAmbientTerm) + vertexDiffuseTerm;
    color = clamp(color, 0.0f, 1.0f);
    float3 objectColor = (ec.rgb * tex.rgb) + (tex.rgb * dc.rgb * color);
    if (FogMode > 0)
    {
        objectColor = ApplyFog(viewPosition, objectColor);
    }
    return float4(objectColor, tex.a);
}

float4 ApplyPixelLighting(
    float4 ac, float4 ec, float4 dc, float4 tex,
    float3 position, float4 viewPosition, float3 normal,
    bool frontFacing
)
{
    if (UseLighting <= 0)
        return dc * tex;
    float3 ambientTerm = float3(0.0, 0.0, 0.0);
    float3 diffuseTerm = float3(0.0, 0.0, 0.0);
    float3 n = frontFacing ? normalize(normal) : normalize(-normal);
    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (i >= int(LightCount))
            break;
        float3 surfaceToLight;
        float attenuation;

        if (Lights[i].Type == 0)
        {
            surfaceToLight = normalize(-Lights[i].Direction);
            attenuation = 1.0;
        }
        else
        {
            surfaceToLight = normalize(Lights[i].Position - position);
            float distanceToLight = length(Lights[i].Position - position);
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
        float diffuseCoefficient = max(dot(n, surfaceToLight), 0.0);
        float3 diffuse = diffuseCoefficient  * Lights[i].Diffuse;
        diffuseTerm += attenuation * diffuse;
        ambientTerm += attenuation * Lights[i].Ambient;
    }
    float3 color = clamp(diffuseTerm + ac.rgb * (AmbientColor + ambientTerm), 0.0f, 1.0f);
    float3 objectColor = (ec.rgb * tex.rgb) + (tex.rgb * dc.rgb * color);
    if (FogMode > 0)
    {
        objectColor = ApplyFog(viewPosition, objectColor);
    }
    return float4(objectColor, tex.a);
}
