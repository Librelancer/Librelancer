@include(includes/sprite.inc)
@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec4 vertex_color;
in vec2 vertex_texture1;

out vec2 Vertex_UV;
out vec4 Vertex_Color;

void main()
{
	vec4 pos = ViewProjection * vec4(vertex_position, 1.0);
	gl_Position = pos;
	Vertex_UV = vertex_texture1;
	Vertex_Color = vertex_color;
}

