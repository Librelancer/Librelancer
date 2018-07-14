in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;

out vec2 out_texcoord;
out vec2 texcoord2;

uniform mat4x4 World;
uniform mat4x4 View;
uniform mat4x4 ViewProjection;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;

	vec4 p = vec4(vertex_position, 1.0);

	vec3 N = normalize(mat3(View * World) * vertex_normal);
	vec3 V = normalize(-vec3((View * World) * p));
    float facingRatio = dot(V, N);
    texcoord2 = vec2(facingRatio, 0);

	out_texcoord = vec2(vertex_texture1.x, 1 - vertex_texture1.y);
}
