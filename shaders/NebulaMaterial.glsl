@feature VERTEX_DIFFUSE

@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec2 vertex_texture1;
#ifdef VERTEX_DIFFUSE
in vec4 vertex_color;
#endif
out vec2 out_texcoord;
out vec4 frag_vertexcolor;

uniform mat4x4 World;


void main()
{
	gl_Position = (ViewProjection * World) * vec4(vertex_position, 1.0);

	vec2 texcoord = vec2(vertex_texture1.x, vertex_texture1.y);
	#ifdef VERTEX_DIFFUSE
	frag_vertexcolor = vertex_color;
	#else
	frag_vertexcolor = vec4(1);
	#endif
	out_texcoord = texcoord;
}

@fragment
in vec2 out_texcoord;
in vec3 world_position;
in vec4 frag_vertexcolor;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;

void main()
{
	out_color = (texture(DtSampler, out_texcoord)) * frag_vertexcolor;
}
