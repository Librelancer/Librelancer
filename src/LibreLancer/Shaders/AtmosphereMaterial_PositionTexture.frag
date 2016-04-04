#version 140
uniform vec4 Dc;
uniform vec4 Ac;
uniform float Alpha;
uniform float Fade;
uniform float Scale;
uniform sampler2D DtSampler;
uniform vec3 CameraPosition;

in vec2 out_texcoord;
in vec3 world_position;
in vec3 normal;

out vec4 out_color;

void main()
{
	vec4 result = vec4(Dc.xyz * Ac.xyz, Alpha);

	vec3 viewDirection = normalize(world_position - CameraPosition);
	float viewAngle = dot(-viewDirection, normal);
	result *=  Fade - viewAngle;

	out_color = mix(vec4(Ac.xyz, Alpha), result, Scale);
}
