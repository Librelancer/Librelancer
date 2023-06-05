@feature VERTEX_LIGHTING
@vertex
@include (includes/lighting.inc)
@include (includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;
in vec2 vertex_texture1;

out vec2 frag_texcoord;
out vec3 N;
out vec3 V;
out vec3 world_position;
out vec3 out_normal;

uniform mat4x4 World;
uniform mat4x4 NormalMatrix;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;

	vec4 p = vec4(vertex_position, 1.0);

	N = normalize(mat3(View * World) * vertex_normal);
	V = normalize(-vec3((View * World) * p));
	world_position = (World * vec4(vertex_position,1)).xyz;
	out_normal = (NormalMatrix * vec4(vertex_normal,0)).xyz;
	vec4 view_position = (View * World) * vec4(vertex_position,1);
	light_vert(world_position, view_position, out_normal);
}

@fragment
@include (includes/lighting.inc)
uniform vec4 Dc;
uniform vec4 Ac;
uniform float Oc;
uniform float TileRate; // Fade
uniform float Scale;
uniform sampler2D DtSampler;

in vec3 V;
in vec3 N;
in vec3 world_position;
in vec3 out_normal;

out vec4 out_color;

void main()
{
	float facingRatio = clamp(dot(normalize(V), normalize(N)), 0., 1.);
	vec2 texcoord = vec2(facingRatio, 0.);
	vec4 lit = light(Ac, vec4(0.), Dc, vec4(1), world_position, vec4(V,1.), out_normal);
	out_color = vec4(lit.rgb, Oc) * texture(DtSampler, texcoord);
}
