#version 140
//Material code
in vec2 out_texcoord;
in vec3 world_position;
in vec4 frag_vertexcolor;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;

void main()
{
	out_color = (texture(DtSampler, out_texcoord));
	out_color *= frag_vertexcolor;
	//out_color = texture(DtSampler, out_texcoord) * Dc;
	//out_color = vec4(0,0,0,0);
}