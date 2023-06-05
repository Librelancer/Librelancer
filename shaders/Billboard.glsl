@include(includes/sprite.inc)
@vertex
@include(includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_dimensions;
in vec4 vertex_color;

in vec2 vertex_texture1;

out vec2 Vertex_UV;
out vec4 Vertex_Color;

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
	//Billboard calculation
	float s = sin(vertex_dimensions.z);
	float c = cos(vertex_dimensions.z);
	vec3 up = c * srcRight - s * srcUp;
	vec3 right = s * srcRight + c * srcUp;
	vec3 pos = vertex_position + (right * vertex_dimensions.x) + (up * vertex_dimensions.y);
	gl_Position = ViewProjection * vec4(pos, 1);
	//pass-through to fragment
	Vertex_UV = vertex_texture1;
	Vertex_Color = vertex_color;
}
