#version 140
uniform vec4 Dc;
uniform vec4 Ac;
uniform float Alpha;
uniform float Fade;
uniform float Scale;
uniform sampler2D DtSampler;

in vec2 out_texcoord;
in vec3 world_position;
out vec4 out_color;
void main()
{
	vec4 result = texture(DtSampler, out_texcoord);
	result.xyz *= Dc.xyz * Ac.xyz;
	result.a *= Alpha;
	out_color = mix(vec4(Ac.xyz, Alpha), result, Scale);
}
