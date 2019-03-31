in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;

out vec2 out_texcoord;
out vec3 N;
out vec3 V;

uniform mat4x4 World;
uniform mat4x4 View;
uniform mat4x4 ViewProjection;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;

	vec4 p = vec4(vertex_position, 1.0);
    
    mat4 modelview = mat4(View * World);
    mat4 normmat = transpose(inverse(modelview));
    
	
    N = normalize(vec3(normmat * vec4(vertex_normal,0.0)));
    V = -vec3(modelview * vec4(vertex_position,1.0));

	out_texcoord = vec2(vertex_texture1.x, 1 - vertex_texture1.y);
}
