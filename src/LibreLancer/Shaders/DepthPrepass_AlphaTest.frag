out vec4 out_color;
in vec2 out_texcoord;
uniform sampler2D DtSampler;

void main()
{
	vec4 sampler = texture(DtSampler, out_texcoord);
	if(sampler.a < 1.0) {
		discard;
	}
	out_color = vec4(1.);
}