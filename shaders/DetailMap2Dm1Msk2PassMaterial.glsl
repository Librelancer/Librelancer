@include(includes/PositionTextureFlip.inc)

@fragment
@include(includes/lighting.inc)
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

@include(includes/modulate.inc)

void main()
{
	vec2 texcoord = out_texcoord;

    vec4 tex = texture(DtSampler, texcoord);

	texcoord *= TileRate;

	vec4 base_color = light(Ac, vec4(0), Dc, tex, world_position, view_position, out_normal);
	
	vec4 col = tex.a < 0.99 ? modulate2x(texture(Dm1Sampler, texcoord), base_color) : base_color;
    out_color = vec4(col.rgb, 1.0);
}
