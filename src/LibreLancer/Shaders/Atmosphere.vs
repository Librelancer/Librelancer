#version 140

in vec3 vertex_position;

out vec3 world_position;
out vec3 normal;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	world_position = (vec4(vertex_position,1) * World).xyz;
	normal = (vec4(normalize(vertex_position),1) * World).xyz;
}

