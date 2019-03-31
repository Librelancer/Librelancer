in vec2 out_texcoord;
in vec3 N;
in vec3 V;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;
uniform sampler2D DmSampler; //Nt_Sampler

void main()
{

    float ratio = (dot(normalize(V),normalize(N)) + 1.0) / 2.0;
    ratio = clamp(ratio,0.0,1.0);
	vec4 nt = texture(DmSampler, vec2(ratio,0));
	vec4 color = texture(DtSampler, out_texcoord);

	out_color = vec4(color.rgb + nt.rgb, color.a * nt.a);
}