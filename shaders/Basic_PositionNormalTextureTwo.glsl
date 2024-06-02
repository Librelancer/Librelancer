@feature NORMALMAP
@include(Basic_Features.inc)
@include(Basic_Fragment.inc)

@vertex
@include(includes/lighting.inc)
@include(includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;
in vec2 vertex_texture2;

out vec2 out_texcoord;
out vec2 out_texcoord2;
#ifdef NORMALMAP
out mat3 tbn;
in vec2 vertex_texture3;
in vec2 vertex_texture4;
#else
out vec3 out_normal;
#endif
out vec3 world_position;
out vec4 out_vertexcolor;
out vec4 view_position;

uniform mat4x4 World;
uniform mat4x4 NormalMatrix;
uniform vec4 MaterialAnim;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	world_position = (World * vec4(vertex_position,1)).xyz;
	view_position = (View * World) * vec4(vertex_position,1);
    vec3 n = (NormalMatrix * vec4(vertex_normal,0)).xyz;
    #ifdef NORMALMAP
    vec4 v_tangent = vec4(vertex_texture3.x, vertex_texture3.y, vertex_texture4.x, vertex_texture4.y);
    vec3 normalW = normalize(vec3(NormalMatrix * vec4(vertex_normal.xyz, 0.0)));
    vec3 tangentW = normalize(vec3(NormalMatrix * vec4(v_tangent.xyz, 0.0)));
    vec3 bitangentW = cross(normalW, tangentW) * v_tangent.w;
    tbn = mat3(tangentW, bitangentW, normalW);
    #else
    out_normal = n;
    #endif
    out_texcoord = vec2(
		(vertex_texture1.x + MaterialAnim.x) * MaterialAnim.z, 
		(vertex_texture1.y + MaterialAnim.y) * MaterialAnim.w
	);
	out_vertexcolor = vec4(1);
	out_texcoord2 = vec2(vertex_texture2.x, vertex_texture2.y);
    light_vert(world_position, view_position, n);
} 
