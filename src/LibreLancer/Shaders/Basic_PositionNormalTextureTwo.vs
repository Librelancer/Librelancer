#version 140

in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;
in vec2 vertex_texture2;

out vec2 out_texcoord1;
out vec2 out_texcoord2;
out vec3 out_normal;
out vec3 world_position;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	world_position = (vec4(vertex_position,1) * World).xyz;
	out_normal = (vec4(vertex_normal,1) * World).xyz;
	out_texcoord1 = vec2(vertex_texture1.x, 1 - vertex_texture1.y);
	out_texcoord2 = vec2(vertex_texture2.x, 1 - vertex_texture2.y);
}
