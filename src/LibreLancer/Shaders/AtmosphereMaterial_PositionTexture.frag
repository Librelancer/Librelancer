#version 140
#pragma include (lighting.inc)
uniform vec4 Dc;
uniform vec4 Ac;
uniform float Alpha;
uniform float Fade;
uniform float Scale;
uniform sampler2D DtSampler;
uniform vec3 CameraPosition;

in vec3 e;
in vec3 n;
in vec3 out_normal;
in vec3 world_position;
out vec4 out_color;
#define FADE_RADIUS 0.95
void main()
{
	vec3 r = reflect ( e, n );
	float m = 2.0 *  sqrt(
		pow (r.x, 2.0) +
		pow (r.y, 2.0) +
		pow (r.z + 1.0, 2.0)
	);
	vec2 envcoords = r.xy / m + 0.5;
	float mindist = (1 - (Scale - 1));
	float dist = distance(envcoords, vec2(0.5,0.5)) * 2;
	float u_radius = Scale * 2;
	if(dist > FADE_RADIUS) {
		float start = min (FADE_RADIUS, u_radius) / u_radius;
		float intensity = mix(0, start, (1 - dist) * (1 / (1 - FADE_RADIUS)));
		vec4 result = vec4(Dc.rgb * Ac.rgb, intensity * Alpha);
		out_color = result;
	} else {
		float intensity = min (dist, u_radius) / u_radius;
		vec4 result = vec4(Dc.rgb * Ac.rgb, intensity * Alpha);
		out_color = result;
	}

}
