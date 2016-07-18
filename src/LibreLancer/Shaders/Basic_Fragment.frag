#version 140
//Lighting code
#pragma include (lighting.inc)
//Material code
in vec2 out_texcoord;
in vec3 world_position;
in vec3 out_normal;
in vec4 out_vertexcolor;
in vec4 view_position;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;
uniform float Oc;
uniform bool OcEnabled;
void main()
{
	vec4 sampler = texture(DtSampler, out_texcoord);
	vec4 color = light(vec4(1), Ec, Dc * out_vertexcolor, texture(DtSampler, out_texcoord), world_position, view_position, out_normal);
	if(OcEnabled)
		out_color = color * vec4(1,1,1,Oc);
	else
		out_color = vec4(color.rgb, sampler.a);
}


