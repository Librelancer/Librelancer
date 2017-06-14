in vec3 vertex_position;
in vec3 vertex_dimensions;
in vec3 vertex_right;
in vec3 vertex_up;
in vec4 vertex_color;

in vec2 vertex_texture1;

out vec2 Vertex_UV;
out vec4 Vertex_Color;

uniform mat4 View;
uniform mat4 ViewProjection;


void main()
{
	//Billboard calculation
	float s = sin(vertex_dimensions.z);
	float c = cos(vertex_dimensions.z);
	vec3 up = c * vertex_right - s * vertex_up;
	vec3 right = s * vertex_right + c * vertex_up;
	vec3 pos = vertex_position + (right * vertex_dimensions.x) + (up * vertex_dimensions.y);
	gl_Position = ViewProjection * vec4(pos, 1);
	//pass-through to fragment
	Vertex_UV = vertex_texture1;
	Vertex_Color = vertex_color;
}
