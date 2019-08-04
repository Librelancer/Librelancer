uniform sampler2D tex0;
uniform vec4 FogColor;
uniform float FogFactor;
in vec2 uv;
in vec4 innercolor;
in vec4 outercolor;
out vec4 FragColor;

void main(void)
{
	vec4 result = texture(tex0, uv) * innercolor;
	result.rgb = mix(result.rgb, outercolor.rgb, FogFactor);
	FragColor = result;
}

