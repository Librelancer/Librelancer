#define MAX_LIGHTS 9
uniform vec3 LightsDir[MAX_LIGHTS];
uniform vec4 LightsPos[MAX_LIGHTS];
uniform vec3 LightsColor[MAX_LIGHTS];
uniform vec4 LightsAttenuation[MAX_LIGHTS];
uniform int LightsRange[MAX_LIGHTS];
uniform vec4 AmbientColor;
uniform int LightCount;
uniform bool LightingEnabled;
/* 
 * approximate function given in [IGraph]
 * ax^3 + bx^2 + cx + d
 */
float fcubic(float x, vec4 params)
{
	return params.x * pow(x, 3) + params.y * pow(x, 2) + params.z * x + params.w;
}

vec3 apply_light(int i, vec4 dc, vec3 position, vec3 normal)
{
    vec3 surfaceToLight;
    float attenuation;
    if (LightsPos[i].w == 0.0) {
		//directional light
		surfaceToLight = normalize(LightsDir[i]);
		float distanceToLight = length(LightsPos[i].xyz - position);
		attenuation = fcubic(distanceToLight / LightsRange[i], LightsAttenuation[i]);
    } else {
		//point light
		surfaceToLight = normalize(LightsPos[i].xyz - position);
		float distanceToLight = length(LightsPos[i].xyz - position);
		vec3 curve = LightsAttenuation[i].xyz;
		attenuation = 1.0 / (curve.x + curve.y * distanceToLight + curve.z * (distanceToLight * distanceToLight));
    }
    //color calculation
    float diffuseCoefficient = max(dot(normal, surfaceToLight), 0.0);
    vec3 diffuse = diffuseCoefficient * dc.rgb * LightsColor[i];
    return attenuation * diffuse;
}

/*
 * ac : Ambient Color (material)
 * ec : Emissive Color
 * dc : Diffuse Color
 * tex : Diffuse Texture
 * position: World Position
 * normal: World Normal
 */
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