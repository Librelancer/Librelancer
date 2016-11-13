#version 150
uniform sampler2D tex0;
uniform vec4 innercolor;
uniform vec4 outercolor;
uniform float expand;

in vec2 Vertex_UV;
in vec4 Vertex_Color;
out vec4 FragColor;
void main(void)
{
	vec4 tex_sample = texture(tex0, Vertex_UV);
	float dist = distance(vec2(0.5,0.5), Vertex_UV) * 2.;
	vec4 blend_color = mix(innercolor, outercolor, (dist - expand) / (1. - expand));
	FragColor = tex_sample * blend_color;
}

