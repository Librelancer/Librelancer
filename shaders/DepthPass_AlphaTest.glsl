@fragment
out vec4 out_color;
in vec2 out_texcoord;
uniform sampler2D DtSampler;

void main()
{
	vec4 sampler = texture(DtSampler, out_texcoord);
	if(sampler.a < 1.0) {
		discard;
	}
	out_color = vec4(1.);
}

@vertex
@include(includes/camera.inc)
in vec3 vertex_position;
in vec2 vertex_texture1;

out vec2 out_texcoord;

uniform vec4 MaterialAnim;
uniform mat4 World;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	out_texcoord = vec2(
		(vertex_texture1.x + MaterialAnim.x) * MaterialAnim.z, 
		(vertex_texture1.y + MaterialAnim.y) * MaterialAnim.w
	);
}

