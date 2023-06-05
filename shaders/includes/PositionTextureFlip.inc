@feature VERTEX_LIGHTING
@vertex
@include (lighting.inc)
@include (camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;

out vec2 out_texcoord;
out vec3 out_normal;
out vec3 world_position;
out vec4 frag_vertexcolor;
out vec4 view_position;

uniform mat4x4 World;
uniform mat4x4 NormalMatrix;

void main()
{
	gl_Position = (ViewProjection * World) * vec4(vertex_position, 1.0);
	vec2 texcoord = vec2(vertex_texture1.x, vertex_texture1.y);
	world_position = (World * vec4(vertex_position,1)).xyz;
	view_position = (View * World) * vec4(vertex_position,1);
	out_normal = (NormalMatrix * vec4(vertex_normal,0.0)).xyz;
	out_texcoord = texcoord;
	frag_vertexcolor = vec4(1);
	light_vert(world_position, view_position, out_normal);
}
