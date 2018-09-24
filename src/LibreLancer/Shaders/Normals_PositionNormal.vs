in vec3 vertex_position;
in vec3 vertex_normal;

out vec3 out_normal;

uniform mat4x4 World;
uniform mat4x4 ViewProjection;
uniform mat4x4 NormalMatrix;

void main()
{
	vec4 pos = (ViewProjection * World) * vec4(vertex_position, 1.0);
	gl_Position = pos;
	vec3 transformed = (NormalMatrix * vec4(vertex_normal,0)).xyz;
    //output red when model doesn't really have normals
    float sum = vertex_normal.x + vertex_normal.y + vertex_normal.z;
    out_normal = mix(transformed,vec3(1.,0,0),step(2.9, sum));
}
