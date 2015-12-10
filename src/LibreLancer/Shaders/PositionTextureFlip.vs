#version 140

in vec3 vertex_position;
in vec2 vertex_texture1;

out vec2 out_texcoord;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

bool FlipU;
bool FlipV;

void main()
{
	gl_Position = vec4(vertex_position,1);
	vec2 texcoord = vec2(vertex_texture1.x, 1 - vertex_texture1.y);
	if (FlipU) texcoord.x = 1 - texcoord.x;
	if (FlipV) texcoord.y = 1 - texcoord.y;
	out_texcoord = texcoord;
}
