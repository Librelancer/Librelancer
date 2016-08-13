#version 150
in vec3 vertex_position;
in vec2 vertex_size;
in vec4 vertex_color;
in float vertex_angle;

in vec2 vertex_texture1;

out vec2 Vertex_UV;
out vec4 Vertex_Color;

uniform mat4 View;
uniform mat4 ViewProjection;


void main()
{
	//Billboard
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
	float s = sin(vertex_angle);
	float c = cos(vertex_angle);

	vec3 up = c * srcRight - s * srcUp;
	vec3 right = s * srcRight + c * srcUp;

	vec3 v = vertex_position + (right * vertex_size.x) + (up * vertex_size.y);
	gl_Position = ViewProjection * vec4(v, 1);
	//pass-through to fragment
	Vertex_UV = vertex_texture1;
	Vertex_Color = vertex_color;
}
