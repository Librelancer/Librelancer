in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;
out vec2 texcoords;
out vec3 world_position;
out vec3 normal;
out vec4 view_position;

uniform mat4x4 World;
uniform mat4x4 View;
uniform mat4x4 ViewProjection;
uniform mat4x4 NormalMatrix;
void main(void)
{
	gl_Position = (ViewProjection * World) * vec4(vertex_position, 1);
	world_position = (World * vec4(vertex_position, 1)).xyz;
	view_position = (View * World) * vec4(vertex_position,1);
	normal = (NormalMatrix * vec4(vertex_normal, 0)).xyz;
	texcoords = vertex_texture1;
}
