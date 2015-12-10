#version 140

in vec3 vertex_position;
in vec2 vertex_texture1;

out vec3 out_texcoord;
out vec3 out_normal;
out vec3 world_position;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	out_normal = (vec4(normalize(vertex_position.xyz),1) * World).xyz;
	world_position = (vec4(vertex_position,1) * World).xyz;
	out_texcoord = vertex_position;
}

