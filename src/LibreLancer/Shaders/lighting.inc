#define MAX_LIGHTS 9
uniform bool LightingEnabled;

uniform vec4 LightsPos[MAX_LIGHTS];
uniform vec4 LightsColorRange[MAX_LIGHTS]; //xyz: color, w: range
uniform vec4 LightsAttenuation[MAX_LIGHTS];
#ifdef SPOTLIGHT
uniform vec3 SpotlightParams[MAX_LIGHTS]; //x: falloff. y: cos(theta/2), z: cos(phi/2)
uniform vec3 LightsDir[MAX_LIGHTS];
#endif
uniform vec4 AmbientColor;
uniform vec4 FogColor;
uniform int LightCount;
uniform int FogMode;
uniform vec2 FogRange;
/* 
 * approximate function given in [IGraph]
 * ax^3 + bx^2 + cx + d
 */
float fcubic(float x, vec4 params)
{
	float x2 = x * x;
	return params.x * x2 * x + params.y * x2 + params.z * x + params.w;
}

#define FOGMODE_LINEAR 3
#define FOGMODE_EXP 1
#define FOGMODE_EXP2 2

vec3 apply_fog(vec4 view_position, vec3 object_color)
{
	float fogFactor;
	float dist = length(view_position);
	if(FogMode == FOGMODE_EXP) {
		//FogRange - x: density
		fogFactor = 1.0 / exp(dist * FogRange.x);
	} else if (FogMode == FOGMODE_EXP2) {
		//FogRange - x: density
		fogFactor = 1.0 / exp((dist * FogRange.x) * (dist * FogRange.x));
	} else {
		//FogRange - x: near, y: far
		fogFactor = (FogRange.y - dist) / (FogRange.y - FogRange.x);
	}
	fogFactor = clamp(fogFactor, 0.0, 1.0);
	return mix(FogColor.rgb, object_color, fogFactor);
}

/*
 * ac : Ambient Color (material)
 * ec : Emissive Color
 * dc : Diffuse Color
 * tex : Diffuse Texture
 * position: World Position
 * view_position: View Position
 * normal: World Normal
 */
vec4 light(vec4 ac, vec4 ec, vec4 dc, vec4 tex, vec3 position, vec4 view_position, vec3 normal)
{
	if(!LightingEnabled)
		return dc * tex;
	vec3 color = AmbientColor.rgb * ac.rgb;
	vec3 n = normalize(normal);
	for(int i = 0; i < MAX_LIGHTS; i++)
	{
		if(i >= LightCount) break;
		vec3 surfaceToLight;
		float attenuation;
		//LightsPos[i].w is the type of light
		//0: directional, 1: point, 2: pointattencurve
		if (LightsPos[i].w == 0.0) { 
			//directional light: LightsPos[i].xyz is direction
			surfaceToLight = normalize(-LightsPos[i].xyz);
			attenuation = 1.;
		} else { 
			//point light
			surfaceToLight = normalize(LightsPos[i].xyz - position);
			float distanceToLight = length(LightsPos[i].xyz - position);
			vec3 curve = LightsAttenuation[i].xyz;
			float atten0 = attenuation = 1.0 / (curve.x + curve.y * distanceToLight + curve.z * (distanceToLight * distanceToLight));
			float atten1 = fcubic(distanceToLight / max(LightsColorRange[i].w,1.), LightsAttenuation[i]);
			attenuation = mix(atten0, atten1, LightsPos[i].w - 1.0); //choose correct attenuation
			#ifdef SPOTLIGHT
			if(SpotlightParams[i].x > 0.0) { //It's a spotlight
				float NdotL = max(dot(n, LightsDir[i]), 0.0);
				if(NdotL > 0.0) {
					float rho = dot(surfaceToLight, -LightsDir[i]);
					attenuation *= pow(clamp((rho - SpotlightParams[i].z) / (SpotlightParams[i].y - SpotlightParams[i].z),0.,1.), SpotlightParams[i].x);
				} else {
					attenuation = 0.0;
				}

			}
			#endif
		}
		//color calculation
		float diffuseCoefficient = max(dot(n, surfaceToLight), 0.0);
		vec3 diffuse = diffuseCoefficient * dc.rgb * LightsColorRange[i].xyz;
		color += attenuation * diffuse;
	}
	color = clamp(color,0.0,1.0);
	vec3 object_color = (ec.rgb * tex.rgb) + (tex.rgb * color);
	if(FogMode > 0) {
		object_color = apply_fog(view_position, object_color);
	}
	return vec4(object_color, tex.a);
}