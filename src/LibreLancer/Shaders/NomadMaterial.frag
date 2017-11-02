in vec2 out_texcoord;
in vec3 V;
in vec3 N;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;
uniform sampler2D DmSampler; //Nt_Sampler
uniform float Oc;

void main()
{
	float facingRatio = clamp(dot(normalize(V), normalize(N)), 0, 1);
	vec2 texcoord2 = vec2(facingRatio, 0);
	vec4 nt = texture(DmSampler, texcoord2);
	vec4 color = texture(DtSampler, out_texcoord);

	out_color = vec4(color.rgb + nt.rgb, color.a * Oc * nt.a);
}