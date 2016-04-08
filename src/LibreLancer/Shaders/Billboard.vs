#version 150
in vec3 vertex_position;
in vec2 vertex_size;
in vec4 vertex_color;
in float vertex_angle;

in vec2 vertex_texture1;
in vec2 vertex_texture2;
in vec2 vertex_texture3;
in vec2 vertex_texture4;

out Vertex
{
	vec4 color;
	vec2 size;
	vec2 texture0;
	vec2 texture1;
	vec2 texture2;
	vec2 texture3;
	float angle;
} vertex;


void main()
{
	gl_Position = vec4(vertex_position, 1);
	vertex.color = vertex_color;
	vertex.size = vertex_size;
	vertex.texture0 = vertex_texture1;
	vertex.texture1 = vertex_texture2;
	vertex.texture2 = vertex_texture3;
	vertex.texture3 = vertex_texture4;
	vertex.angle = vertex_angle;
}
