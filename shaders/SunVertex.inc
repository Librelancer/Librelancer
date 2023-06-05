@vertex
@include(includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_dimensions;
in vec4 vertex_color;
in vec4 vertex_color2;

in vec2 vertex_texture1;

out vec2 uv;
out vec4 innercolor;
out vec4 outercolor;

uniform vec2 SizeMultiplier;

void main()
{
    //Up/right
    vec3 srcRight = vec3(
        View[0][0],
        View[1][0],
        View[2][0]
    );
    vec3 srcUp = vec3(
        View[0][1],
        View[1][1],
        View[2][1]
    );
    vec2 dimensions = SizeMultiplier * vertex_dimensions.xy;
    //Billboard calculation
    float s = sin(vertex_dimensions.z);
    float c = cos(vertex_dimensions.z);
    vec3 up = c * srcRight - s * srcUp;
    vec3 right = s * srcRight + c * srcUp;
    vec3 pos = vertex_position + (right * dimensions.x) + (up * dimensions.y);
    gl_Position = ViewProjection * vec4(pos, 1);
    //pass-through to fragment
    uv = vertex_texture1;
    innercolor = vertex_color;
    outercolor = vertex_color2;
}
