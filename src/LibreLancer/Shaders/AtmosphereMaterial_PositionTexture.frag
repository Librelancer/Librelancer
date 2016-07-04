#version 140
uniform vec4 Dc;
uniform vec4 Ac;
uniform float Alpha;
uniform float Fade;
uniform float Scale;
uniform sampler2D DtSampler;
uniform vec3 CameraPosition;

in vec3 e;
in vec3 n;

out vec4 out_color;

void main()
{
	vec3 r = reflect ( e, n );
	float m = 2.0 *  sqrt(
		pow (r.x, 2.0) +
		pow (r.y, 2.0) +
		pow (r.z + 1.0, 2.0)
	);
	vec2 envcoords = r.xy / m + 0.5;
	float dist = distance(envcoords, vec2(0.5,0.5));
	vec4 result = vec4(Dc.rgb * Ac.rgb, dist * 2 * Alpha);
	out_color = result;
}
