@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;

out vec3 out_normal;

uniform mat4x4 World;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
    //output red when model doesn't really have normals
    float sum = vertex_normal.x + vertex_normal.y + vertex_normal.z;
    out_normal = mix(vertex_normal,vec3(1.,0,0),step(2.9, sum));
}

@fragment
out vec4 out_color;
in vec3 out_normal;

void main()
{
	vec3 n = normalize(out_normal);
	out_color = vec4(n * 0.5 + 0.5, 1);
}
