in vec3 vertex_position;
in vec3 vertex_normal;

out vec3 out_normal;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;
uniform mat4x4 NormalMatrix;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	out_normal = (NormalMatrix * vec4(vertex_normal,0)).xyz;
}
