#define MAX_LIGHTS 9
uniform vec3 LightsDir[MAX_LIGHTS];
uniform vec4 LightsPos[MAX_LIGHTS];
uniform vec3 LightsColor[MAX_LIGHTS];
uniform int LightsRange[MAX_LIGHTS];
uniform vec4 AmbientColor;
uniform int LightCount;
uniform bool LightingEnabled;

vec3 apply_light(int i, vec4 ec, vec4 dc, vec3 position, vec3 normal)
{
    vec3 surfaceToLight;
    float attenuation;
    if (LightsPos[i].w == 0.0) {
		//directional light
		surfaceToLight = normalize(LightsPos[i].xyz);
		attenuation = 1.0;
    } else {
		//point light
		surfaceToLight = normalize(LightsPos[i].xyz - position);
		float distanceToLight = length(LightsPos[i].xyz - position);
		//attenuation = 1 / (1.0 * pow(distanceToLight / LightsRange[i], 2));
		attenuation = 2;
    }
    //color calculation
    float diffuseCoefficient = max(dot(normal, surfaceToLight), 0.0);
    vec3 diffuse = diffuseCoefficient * dc.rgb * LightsColor[i];
    return attenuation * diffuse;
}

vec4 light(vec4 ec, vec4 dc, vec3 position, vec3 normal)
{
	if(!LightingEnabled)
		return dc;
	vec3 result = AmbientColor.rgb * dc.rgb;
	vec3 n = normalize(normal);
	for(int i = 0; i < LightCount; i++)
	{
		result += apply_light(i, ec, dc, position, n);
	}
	return vec4(result, dc.a);
}