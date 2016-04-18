#version 150
uniform sampler2D tex0;
in vec2 Vertex_UV;
in vec4 Vertex_Color;
out vec4 FragColor;
void main(void)
{
	FragColor = texture(tex0, Vertex_UV) * Vertex_Color;
}
