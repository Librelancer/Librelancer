#version 140

in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;
in vec2 vertex_texture2;

out vec2 out_texcoord;
out vec3 out_normal;
out vec3 world_position;
out vec4 out_vertexcolor;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;
uniform mat4x4 NormalMatrix;
void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	world_position = (World * vec4(vertex_position,1)).xyz;
	out_normal = (NormalMatrix * vec4(vertex_normal,0)).xyz;
	out_texcoord = vec2(vertex_texture1.x, 1 - vertex_texture1.y);
	out_vertexcolor = vec4(1);
	//out_texcoord2 = vec2(vertex_texture2.x, 1 - vertex_texture2.y);
}
