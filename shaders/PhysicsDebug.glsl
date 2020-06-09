@lazy
@fragment
in vec4 frag_vertexcolor;
out vec4 out_color;
void main()
{
	out_color = frag_vertexcolor;
}

@vertex
in vec3 vertex_position;
in vec4 vertex_color;

out vec4 frag_vertexcolor;

uniform mat4x4 ViewProjection;

void main()
{
	gl_Position = (ViewProjection) * vec4(vertex_position, 1.0);
	frag_vertexcolor = vertex_color;
}
