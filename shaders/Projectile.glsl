@include (includes/sprite.inc)
@lazy
@vertex
in vec3 vertex_position;
in vec4 vertex_color;
in vec2 vertex_texture1;

out vec2 Vertex_UV;
out vec4 Vertex_Color;

uniform mat4 ViewProjection;

void main()
{
    gl_Position = ViewProjection * vec4(vertex_position, 1);
    //pass-through to fragment
    Vertex_UV = vertex_texture1;
    Vertex_Color = vertex_color;
}

