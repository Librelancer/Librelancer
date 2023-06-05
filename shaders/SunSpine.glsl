@include(SunVertex.inc)
@fragment
uniform sampler2D DtSampler;
in vec2 uv;
in vec4 innercolor;
in vec4 outercolor;
out vec4 FragColor;

void main(void)
{
	vec4 tex_sample = texture(DtSampler, uv);
	float dist = abs(0.5 - uv.y) * 2.;
	vec4 blend_color = mix(innercolor, outercolor, dist);
	FragColor = tex_sample * blend_color;
}
