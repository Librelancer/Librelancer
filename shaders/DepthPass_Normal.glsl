@fragment
out vec4 out_color;
void main()
{
	out_color = vec4(1.);
}

@vertex
@include (includes/camera.inc)
in vec3 vertex_position;

uniform mat4x4 World;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
}
