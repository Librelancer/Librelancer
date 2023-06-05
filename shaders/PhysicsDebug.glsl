@fragment
in vec4 frag_vertexcolor;
out vec4 out_color;
void main()
{
	out_color = frag_vertexcolor;
}

@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec4 vertex_color;

out vec4 frag_vertexcolor;

void main()
{
	gl_Position = (ViewProjection) * vec4(vertex_position, 1.0);
	frag_vertexcolor = vertex_color;
}
