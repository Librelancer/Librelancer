#version 140

in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;

out vec3 e;
out vec3 n;
out vec3 world_position;
out vec3 out_normal;
uniform mat4x4 World;
uniform mat4x4 View;
uniform mat4x4 ViewProjection;
uniform mat4x4 NormalMatrix;
uniform vec3 CameraPosition;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;

	vec4 p = vec4(vertex_position, 1.0);

	e = normalize(vec3(View * World * p));
	n = normalize(vec3(NormalMatrix * vec4(vertex_normal, 0.0)));
	out_normal = (NormalMatrix * vec4(vertex_normal, 0.0)).xyz;
	world_position = (World * vec4(vertex_position, 1.0)).xyz;
}

