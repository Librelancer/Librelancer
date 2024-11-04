@vertex
in vec2 vertex_position;
uniform mat4x4 ViewProjection;
uniform mat4x4 World;

void main()
{
    gl_Position = (ViewProjection * World) * vec4(vertex_position, 0.0, 1.0);
}

@fragment
uniform vec4 Dc;
uniform vec4 Rectangle;
uniform vec2 Tiling;
uniform sampler2D DtSampler;

out vec4 out_color;

void main()
{
    vec2 uv = (gl_FragCoord.xy - Rectangle.xy) / Rectangle.zw;
    uv *= Tiling;
    out_color = texture(DtSampler, uv) * Dc;
}
