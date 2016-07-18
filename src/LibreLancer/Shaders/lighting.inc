#define MAX_LIGHTS 9
uniform vec3 LightsDir[MAX_LIGHTS];
uniform vec4 LightsPos[MAX_LIGHTS];
uniform vec3 LightsColor[MAX_LIGHTS];
uniform vec3 LightsAttenuation[MAX_LIGHTS];
uniform int LightsRange[MAX_LIGHTS];
uniform vec4 AmbientColor;
uniform int LightCount;
uniform bool LightingEnabled;

vec3 apply_light(int i, vec4 dc, vec3 position, vec3 normal)
{
    vec3 surfaceToLight;
    float attenuation;
    if (LightsPos[i].w == 0.0) {
		//directional light
		surfaceToLight = normalize(LightsDir[i]);
		attenuation = 1.0;
    } else {
		//point light
		surfaceToLight = normalize(LightsPos[i].xyz - position);
		float distanceToLight = length(LightsPos[i].xyz - position);
		vec3 curve = LightsAttenuation[i];
		attenuation = 1.0 / (curve.x + curve.y * distanceToLight + curve.z * (distanceToLight * distanceToLight));
    }
    //color calculation
    float diffuseCoefficient = max(dot(normal, surfaceToLight), 0.0);
    vec3 diffuse = diffuseCoefficient * dc.rgb * LightsColor[i];
    return attenuation * diffuse;
}

vec4 light(vec4 ac, vec4 ec, vec4 dc, vec4 tex, vec3 position, vec3 normal)
{
	if(!LightingEnabled)
		return dc * tex;
	vec3 diffuse = AmbientColor.rgb * ac.rgb;
	vec3 n = normalize(normal);
	for(int i = 0; i < LightCount; i++)
	{
		diffuse += apply_light(i, dc, position, n);
	}
	return vec4(
		ec.rgb + (tex.rgb * diffuse),
		tex.a
	);
}