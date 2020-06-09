@include(includes/PositionTextureFlip.inc)

@fragment
@include(includes/lighting.inc)
uniform sampler2D DtSampler;
uniform sampler2D DmSampler;
uniform vec4 Dc;
uniform vec4 Ac;
uniform float TileRate;

in vec2 out_texcoord;
out vec4 out_color;
in vec3 out_normal;
in vec3 world_position;
in vec4 view_position;

@include(includes/blend_overlay.inc)

void main()
{
	vec2 texcoord = out_texcoord;

	vec4 tex = texture(DtSampler, texcoord);

	texcoord *= TileRate;
	tex = blend_overlay(texture(DmSampler, texcoord), tex);

	out_color = light(Ac, vec4(0), Dc, tex, world_position, view_position, out_normal);
}
