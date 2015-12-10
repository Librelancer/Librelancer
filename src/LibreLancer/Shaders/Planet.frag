#version 140
//Lighting code
uniform vec3 LightsPos[9];
uniform vec3 LightsRotate[9];
uniform vec4 LightsColor[9];
uniform int LightsRange[9];
uniform vec3 LightsAttenuation[9];
uniform vec4 AmbientColor = vec4 (1.0, 1.0, 1.0, 1.0);
uniform int LightCount = 0;
vec4 light(vec4 ec, vec4 dc, vec3 position, vec3 normal)
{
	vec4 result = AmbientColor;
	for (int i = 0; i < LightCount; i++) {
		int dist = int(distance(LightsPos[i], position));
		if(LightsRange[i] >= dist) {
			float lightAttenuation = clamp(1 / (LightsAttenuation[i].x + LightsAttenuation[i].y * dist + LightsAttenuation[i].z * dist * dist), 0.0, 1.0);
			vec3 lightDirection = normalize(LightsPos[i] - position);
			float lightAngle = max(0, dot(lightDirection, normal));
			result += lightAttenuation * lightAngle * LightsColor[i];
		}
	}
	return ec + (dc * clamp (result, 0.0, 1.0));
}
//Material code
in vec3 out_texcoord;
in vec3 world_position;
in vec3 out_normal;

out vec4 out_color;
uniform samplerCube planetTexture;

void main()
{
	vec4 dc = texture(planetTexture, out_texcoord);
	out_color = dc;
	//out_color = light(vec4(0,0,0,0), dc, world_position, out_normal);
	//out_color = texture(DtSampler, out_texcoord) * Dc;

	//out_color = vec4(1,0,0,1);
}


