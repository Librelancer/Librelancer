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

uniform bool Fade;
uniform vec2 FadeRange;
void main()
{
	vec4 sampler = texture(DtSampler, out_texcoord);
	vec4 color = light(vec4(1), Ec, Dc * out_vertexcolor, texture(DtSampler, out_texcoord), world_position, view_position, out_normal);
	vec4 acolor;
	if (OcEnabled)
		acolor = color * vec4(1,1,1,Oc);
	else
		acolor = vec4(color.rgb, sampler.a);
	if(Fade) {
		float dist = length(view_position);
		//FadeRange - x: near, y: far
		float fadeFactor = (FadeRange.y - dist) / (FadeRange.y - FadeRange.x);
		fadeFactor = clamp(fadeFactor, 0.0, 1.0);
		//fade
		out_color = vec4(acolor.rgb, acolor.a * fadeFactor);
	} else {
		out_color = acolor;
	}
}


