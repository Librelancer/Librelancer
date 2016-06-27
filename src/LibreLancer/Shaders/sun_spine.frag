#version 150
uniform sampler2D tex0;
uniform vec3 innercolor;
uniform vec3 outercolor;
uniform float alpha;

in vec2 Vertex_UV;
in vec4 Vertex_Color;
out vec4 FragColor;
void main(void)
{
	vec4 tex_sample = texture(tex0, Vertex_UV);
	float dist = distance(vec2(0.5,0.5), Vertex_UV) * 2;
	vec3 blend_color = mix(innercolor, outercolor, dist);
	FragColor = tex_sample * vec4(blend_color, alpha);
}

