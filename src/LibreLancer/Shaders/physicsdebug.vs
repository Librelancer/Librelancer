#version 140

in vec3 vertex_position;
in vec4 vertex_color;

out vec4 frag_vertexcolor;

uniform mat4x4 ViewProjection;

void main()
{
	gl_Position = (ViewProjection) * vec4(vertex_position, 1.0);
	frag_vertexcolor = vertex_color;
}
