#version 140

uniform sampler2D DtSampler;
uniform sampler2D DmSampler;
uniform vec4 Dc;
uniform vec4 Ac;
uniform float TileRate;

in vec2 out_texcoord;
out vec4 out_color;

void main()
{
	vec2 texcoord = out_texcoord;

	vec4 dc = texture(DtSampler, texcoord);
	dc *= Dc;

	texcoord *= TileRate;
	dc *= texture(DmSampler, texcoord);

	out_color = Ac * dc;
}
