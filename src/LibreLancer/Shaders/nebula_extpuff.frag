uniform sampler2D tex0;
uniform vec4 FogColor;
uniform float FogFactor;
in vec2 Vertex_UV;
in vec4 Vertex_Color;
out vec4 FragColor;

void main(void)
{
	vec4 result = texture(tex0, Vertex_UV) * Vertex_Color;
	result.rgb = mix(result.rgb, FogColor.rgb, FogFactor);
	FragColor = result;
}

