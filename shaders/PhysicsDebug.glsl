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

uniform vec4 Dc;
uniform float Oc;
uniform mat4 World;

void main()
{
	gl_Position = (ViewProjection * World) * vec4(vertex_position, 1.0);
	frag_vertexcolor = Oc == 0.0 ? vertex_color : Dc;
}
