#version 150
uniform sampler2D tex0;
in vec2 Vertex_UV;
in vec4 Vertex_Color;
out vec4 FragColor;
void main(void)
{
	vec2 uv = Vertex_UV.xy;
	uv.y *= -1.0;
	vec3 t = texture(tex0, uv).rgb;
	FragColor = vec4(t,1.0) * Vertex_Color;
}
