uniform sampler2D tex0;
in vec4 innercolor;
in vec4 outercolor;
in vec2 uv;

out vec4 FragColor;
void main(void)
{
	vec4 tex_sample = texture(tex0, uv);
	float dist = abs(0.5 - uv.y) * 2.;
	vec4 blend_color = mix(innercolor, outercolor, dist);
	FragColor = tex_sample * blend_color;
}

