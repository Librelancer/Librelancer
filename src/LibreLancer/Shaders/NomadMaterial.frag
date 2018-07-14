in vec2 out_texcoord;
in vec2 texcoord2;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;
uniform sampler2D DmSampler; //Nt_Sampler
uniform float Oc;

void main()
{

	vec4 nt = texture(DmSampler, texcoord2);
	vec4 color = texture(DtSampler, out_texcoord);

	out_color = vec4(color.rgb + nt.rgb, color.a * Oc * nt.a);
}