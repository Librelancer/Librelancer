@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;

out vec2 out_texcoord;
out vec3 N;
out vec3 V;

uniform mat4x4 World;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;

	vec4 p = vec4(vertex_position, 1.0);
    
    mat4 modelview = mat4(View * World);
    mat4 normmat = transpose(inverse(modelview));
    
	
    N = normalize(vec3(normmat * vec4(vertex_normal,0.0)));
    V = -vec3(modelview * vec4(vertex_position,1.0));

	out_texcoord = vec2(vertex_texture1.x, 1.0 - vertex_texture1.y);
}

@fragment
in vec2 out_texcoord;
in vec3 N;
in vec3 V;

out vec4 out_color;
uniform vec4 Dc;
uniform vec4 Ec;
uniform sampler2D DtSampler;
uniform sampler2D DmSampler; //Nt_Sampler

void main()
{

    float ratio = (dot(normalize(V),normalize(N)) + 1.0) / 2.0;
    ratio = clamp(ratio,0.0,1.0);
	vec4 nt = texture(DmSampler, vec2(ratio,0));
	vec4 color = texture(DtSampler, out_texcoord);

	out_color = vec4(color.rgb + nt.rgb, color.a * nt.a);
}
