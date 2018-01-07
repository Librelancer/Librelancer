in vec3 vertex_position;
in vec2 vertex_texture1;

out vec2 out_texcoord;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;
uniform vec4 MaterialAnim;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	out_texcoord = vec2(
		(vertex_texture1.x + MaterialAnim.x) * MaterialAnim.z, 
		1. - (vertex_texture1.y + MaterialAnim.y) * MaterialAnim.w
	);
}
