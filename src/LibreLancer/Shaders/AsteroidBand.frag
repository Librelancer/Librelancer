#version 150
out vec4 out_color;
in vec2 texcoords;
in vec3 world_position;
in vec3 normal;
uniform sampler2D Texture;
uniform vec3 CameraPosition;

#define FADE_DISTANCE 12000

void main(void)
{
	float dist = distance(CameraPosition, world_position);
	float delta = max(FADE_DISTANCE - dist, 0.0);
	float alpha = (FADE_DISTANCE - delta) / FADE_DISTANCE;
	vec4 tex = texture(Texture, texcoords);
	out_color = vec4(tex.rgb, tex.a * alpha);
}