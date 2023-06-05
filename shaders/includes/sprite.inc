@fragment
uniform sampler2D DtSampler;
in vec2 Vertex_UV;
in vec4 Vertex_Color;
out vec4 FragColor;
void main(void)
{
	FragColor = texture(DtSampler, Vertex_UV) * Vertex_Color;
}

 
