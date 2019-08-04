uniform sampler2D tex0;
uniform float expand;

in vec2 uv;
in vec4 innercolor;
in vec4 outercolor;
out vec4 FragColor;
void main(void)
{
	vec4 tex_sample = texture(tex0, uv);
	float dist = distance(vec2(0.5,0.5), uv) * 2.;
	vec4 blend_color = mix(innercolor, outercolor, dist);
	FragColor = tex_sample * blend_color;
}

