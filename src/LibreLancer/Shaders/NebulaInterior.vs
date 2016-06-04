#version 150

in vec3 vertex_position;
in vec2 vertex_texture1;

out vec2 texcoord;
uniform mat4x4 ViewProjection;
uniform mat4x4 World;
void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	texcoord = vertex_texture1;
}
