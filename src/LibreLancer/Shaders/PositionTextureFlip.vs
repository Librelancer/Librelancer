#version 140

in vec3 vertex_position;
in vec2 vertex_texture1;

out vec2 out_texcoord;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;


//uniform int FlipU;
//uniform int FlipV;

void main()
{
	gl_Position = (ViewProjection * World) * vec4(vertex_position, 1.0);

	vec2 texcoord = vec2(vertex_texture1.x, vertex_texture1.y);
	//texcoord.x *= FlipU;
	//texcoord.y *= FlipV;

	out_texcoord = texcoord;
}
