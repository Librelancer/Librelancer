@fragment
out vec4 out_color;
void main()
{
	out_color = vec4(1.);
}

@vertex
in vec3 vertex_position;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
}
