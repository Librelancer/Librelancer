@include(SunVertex.inc)
@fragment
uniform sampler2D DtSampler;
in vec2 uv;
in vec4 innercolor;
in vec4 outercolor;
out vec4 FragColor;
uniform float outerAlpha;
void main(void)
{
	vec4 tex_sample = texture(DtSampler, uv);
	float dist = distance(vec2(0.5,0.5), uv) * 2.;
    vec4 outerC = vec4(outercolor.rgb, outercolor.a * outerAlpha);
	vec4 blend_color = mix(innercolor, outerC, dist);
	FragColor = tex_sample * blend_color;
}

