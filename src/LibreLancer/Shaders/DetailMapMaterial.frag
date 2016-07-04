#version 140
#pragma include (lighting.inc)
uniform sampler2D DtSampler;
uniform sampler2D DmSampler;
uniform vec4 Dc;
uniform vec4 Ac;
uniform float TileRate;

in vec2 out_texcoord;
out vec4 out_color;
in vec3 out_normal;
in vec3 world_position;

void main()
{
	vec2 texcoord = out_texcoord;

	vec4 dc = texture(DtSampler, texcoord);
	dc *= Dc;

	texcoord *= TileRate;
	dc *= texture(DmSampler, texcoord);

	out_color = light(vec4(0), Ac * dc, world_position, out_normal);
}
