out vec4 out_color;
in vec3 out_normal;

void main()
{
	vec3 n = normalize(out_normal);
	out_color = vec4(n.xyz, 1);
}