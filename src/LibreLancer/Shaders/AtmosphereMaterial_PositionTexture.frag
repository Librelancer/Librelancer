#pragma include (lighting.inc)
uniform vec4 Dc;
uniform vec4 Ac;
uniform float Oc;
uniform float TileRate; // Fade
uniform float Scale;
uniform sampler2D DtSampler;
uniform vec3 CameraPosition;

in vec3 V;
in vec3 N;
in vec3 world_position;
in vec3 out_normal;

out vec4 out_color;

void main()
{
	float facingRatio = clamp(dot(normalize(V), normalize(N)), 0, 1);
	vec2 texcoord = vec2(facingRatio, 0);
	vec4 lit = light(Ac, vec4(0), Dc, vec4(1), world_position, vec4(V,1), out_normal);
	out_color = vec4(lit.rgb, Oc) * texture(DtSampler, texcoord);
}
