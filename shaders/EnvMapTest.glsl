@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;

out vec3 out_normal;
out vec3 world_position;

uniform mat4x4 World;
uniform mat4x4 NormalMatrix;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
    world_position = (World * vec4(vertex_position,1)).xyz;
	out_normal = (NormalMatrix * vec4(vertex_normal,0)).xyz;
}

@fragment
@include (includes/camera.inc)
in vec3 out_normal;
in vec3 world_position;
out vec4 out_color;

uniform samplerCube Cubemap;

void main()
{
    vec3 I = normalize(world_position - CameraPosition);
    vec3 R = reflect(I, normalize(out_normal));
    
    out_color = vec4(texture(Cubemap, R).rgb, 1.0);
}
