@fragment
//Lighting code
@include (includes/lighting.inc)
//Material code
in vec2 out_texcoord;
in vec3 world_position;
#ifdef NORMALMAP
in mat3 tbn;
#else
in vec3 out_normal;
#endif
in vec4 out_vertexcolor;
in vec4 view_position;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;
uniform sampler2D EtSampler;
uniform sampler2D NtSampler;
uniform float Oc;

uniform vec2 FadeRange;

#ifdef NORMALMAP
vec3 getNormal()
{
    vec3 n = texture(NtSampler, out_texcoord).xyz;
    n.xy = (n.xy * 2.0 - 1.0);
    n.z = sqrt(1.0 - dot(n.xy, n.xy));
    n = normalize(n);
    return normalize(tbn * n);
}
#else
vec3 getNormal()
{
    return normalize(out_normal);
}
#endif

void main()
{
	vec4 sampler = texture(DtSampler, out_texcoord);
	#ifdef ALPHATEST_ENABLED
	if(sampler.a < 0.5) {
		discard;
	}
	#endif
	vec4 ec = Ec;
	#ifdef ET_ENABLED
	ec += texture(EtSampler, out_texcoord);
	#endif
	vec4 color = light(vec4(1), ec, Dc * out_vertexcolor, texture(DtSampler, out_texcoord), world_position, view_position, getNormal());
	vec4 acolor = color * vec4(1,1,1,Oc);
	#ifdef FADE_ENABLED
	float dist = length(view_position);
	//FadeRange - x: near, y: far
	float fadeFactor = (FadeRange.y - dist) / (FadeRange.y - FadeRange.x);
	fadeFactor = clamp(fadeFactor, 0.0, 1.0);
	//fade
	out_color = vec4(acolor.rgb, acolor.a * fadeFactor);
	#else
	out_color = acolor;
	#endif
}


 
