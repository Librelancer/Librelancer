@vertex
in vec3 vertex_position;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;

uniform float TileRate0;

void main()
{
    // ring mesh is radius 2.0 outside, radius 1.0 inside
    // TileRate0 will be InnerRadius / OuterRadius
    vec3 p = length(vertex_position.xz) > 1.1 
        ? vec3(vertex_position.x / 2., vertex_position.y, vertex_position.z / 2.)
        : vec3(vertex_position.x * TileRate0, vertex_position.y, vertex_position.z * TileRate0);
    gl_Position = (ViewProjection * World) * vec4(p, 1.0);
}

@fragment
uniform vec4 Dc;
out vec4 out_color;

void main()
{
    out_color = Dc;
}
