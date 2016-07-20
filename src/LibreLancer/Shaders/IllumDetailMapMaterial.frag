#version 140
#pragma include (lighting.inc)
uniform sampler2D DtSampler;
uniform sampler2D Dm0Sampler;
uniform sampler2D Dm1Sampler;
uniform vec4 Dc;
uniform vec4 Ac;
uniform float TileRate0;
uniform float TileRate1;
uniform int FlipU;
uniform int FlipV;

in vec2 out_texcoord;
out vec4 out_color;
in vec3 out_normal;
in vec3 world_position;
in vec4 view_position;

#pragma include (blend_overlay.inc)

void main()
{
	vec2 texcoord = out_texcoord;
	if (FlipU != 0) texcoord.x = 1 - texcoord.x;
	if (FlipV != 0) texcoord.y = 1 - texcoord.y;

	vec4 tex = texture(DtSampler, texcoord);

	vec2 texcoord0 = texcoord * TileRate0;
	vec4 detail0 = texture(Dm0Sampler, texcoord0);

	vec2 texcoord1 = texcoord * TileRate1;
	vec4 detail1 = texture(Dm1Sampler, texcoord1);


	out_color = light(Ac, blend_overlay(detail1, tex) * tex.a, Dc, blend_overlay(detail0, tex), world_position, view_position, out_normal);
}
