#version 150
#pragma include (lighting.inc)
uniform vec4 ColorShift;
uniform float TextureAspect;

out vec4 out_color;
in vec2 texcoords;
in vec3 world_position;
in vec3 normal;
in vec4 view_position;
uniform sampler2D Texture;
uniform vec3 CameraPosition;

#define FADE_DISTANCE 12000.

void main(void)
{
	float dist = distance(CameraPosition, world_position);
	float delta = max(FADE_DISTANCE - dist, 0.0);
	float alpha = (FADE_DISTANCE - delta) / FADE_DISTANCE;
	vec4 tex = texture(Texture, texcoords * vec2(TextureAspect, 1));
	vec4 dc = vec4(tex.rgb * ColorShift.rgb, tex.a * alpha);
	out_color = light(vec4(1), vec4(0), vec4(1), dc, world_position, view_position, normal);
}