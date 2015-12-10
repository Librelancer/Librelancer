#version 140

uniform sampler2D DtSampler;
uniform sampler2D Dm1Sampler;
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
	dc *= mix(texture(Dm1Sampler, texcoord), vec4(1), dc.a);

	out_color = Ac * dc;
}
