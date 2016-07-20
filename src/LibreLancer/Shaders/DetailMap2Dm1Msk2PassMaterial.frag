#version 140
#pragma include (lighting.inc)
uniform sampler2D DtSampler;
uniform sampler2D Dm1Sampler;
uniform vec4 Dc;
uniform vec4 Ac;
uniform float TileRate;

in vec2 out_texcoord;
out vec4 out_color;
in vec3 out_normal;
in vec3 world_position;
in vec4 view_position;

#pragma include (blend_overlay.inc)

void main()
{
	vec2 texcoord = out_texcoord;

    vec4 tex = texture(DtSampler, texcoord);

	texcoord *= TileRate;
	vec4 blend = blend_overlay(texture(Dm1Sampler, texcoord), tex);

	out_color = light(Ac, vec4(0), Dc, mix(blend, tex, tex.a), world_position, view_position, out_normal);
}
