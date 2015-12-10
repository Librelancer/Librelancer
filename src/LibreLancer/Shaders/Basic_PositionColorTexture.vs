#version 140

in vec3 vertex_position;
in vec4 vertex_color;
in vec2 vertex_texture1;

out vec2 out_texcoord;
out vec4 frag_vertexcolor;
out vec3 world_position;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	world_position = pos.xyz;
	frag_vertexcolor = vertex_color;
	out_texcoord = vec2(vertex_texture1.x, 1 - vertex_texture1.y);
	out_texcoord = vertex_texture1;
}
