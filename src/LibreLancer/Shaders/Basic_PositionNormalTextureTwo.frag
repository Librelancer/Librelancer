#version 140
//Lighting code
#pragma include (lighting.inc)
//Material code
in vec2 out_texcoord1;
in vec2 out_texcoord2;
in vec3 world_position;
in vec3 out_normal;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;

void main()
{
	out_color = light(Ec, texture(DtSampler, out_texcoord1) * Dc, world_position, out_normal);
	out_color = texture(DtSampler, out_texcoord1) * Dc;
}


