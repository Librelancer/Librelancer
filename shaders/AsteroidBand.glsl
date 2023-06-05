@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;
out vec2 texcoords;
out vec3 world_position;
out vec3 normal;
out vec4 view_position;

uniform mat4x4 World;
uniform mat4x4 NormalMatrix;

void main(void)
{
	gl_Position = (ViewProjection * World) * vec4(vertex_position, 1);
	world_position = (World * vec4(vertex_position, 1)).xyz;
	view_position = (View * World) * vec4(vertex_position,1);
	normal = (NormalMatrix * vec4(vertex_normal, 0)).xyz;
	texcoords = vertex_texture1;
}

@fragment
@include (includes/lighting.inc)
@include (includes/camera.inc)
uniform vec4 ColorShift;
uniform float TextureAspect;

out vec4 out_color;
in vec2 texcoords;
in vec3 world_position;
in vec3 normal;
in vec4 view_position;
uniform sampler2D DtSampler;

#define FADE_DISTANCE 12000.

void main(void)
{
	float dist = distance(CameraPosition, world_position);
	float delta = max(FADE_DISTANCE - dist, 0.0);
	float alpha = (FADE_DISTANCE - delta) / FADE_DISTANCE;
	vec4 tex = texture(DtSampler, texcoords * vec2(TextureAspect, 1));
	vec4 dc = vec4(tex.rgb * ColorShift.rgb, tex.a * alpha);
	out_color = light(vec4(1), vec4(0), vec4(1), dc, world_position, view_position, normal);
} 
