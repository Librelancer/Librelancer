#version 140

in vec3 vertex_position;
in vec2 vertex_texture1;
in vec4 vertex_color;

out vec2 out_texcoord;
out vec4 frag_vertexcolor;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

void main()
{
	gl_Position = (ViewProjection * World) * vec4(vertex_position, 1.0);

	vec2 texcoord = vec2(vertex_texture1.x, 1 - vertex_texture1.y);
	frag_vertexcolor = vertex_color;
	out_texcoord = texcoord;
}

