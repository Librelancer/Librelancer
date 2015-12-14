#version 140

uniform sampler2D DtSampler;
uniform sampler2D Dm0Sampler;
uniform sampler2D Dm1Sampler;
uniform vec4 Dc;
uniform vec4 Ac;

uniform float TileRate0;
uniform float TileRate1;

in vec2 out_texcoord;
out vec4 out_color;

void main()
{
	vec2 texcoord = out_texcoord;

	vec4 dc = texture(DtSampler, texcoord);

	vec2 texcoord0 = texcoord * TileRate0;
	vec4 detail0 = texture(Dm0Sampler, texcoord0);

	vec2 texcoord1 = texcoord * TileRate1;
	vec4 detail1 = texture(Dm1Sampler, texcoord1);

	vec4 detail = vec4(mix(detail0.xyz, detail1.xyz, dc.a),1);
	dc *= detail;

	out_color =  Ac * dc;
}
