@vertex
@include(includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_normal;

out vec3 worldPos;
out vec3 normal;

uniform mat4x4 World;
uniform mat4x4 NormalMatrix;
uniform float TileRate0;

void main()
{
    // ring mesh is radius 2.0 outside, radius 1.0 inside
    // TileRate0 will be InnerRadius / OuterRadius
    vec3 p = vertex_position;
    if(TileRate0 != 0.0) {
        vec3 p = length(vertex_position.xz) > 1.1 
            ? vec3(vertex_position.x / 2., vertex_position.y, vertex_position.z / 2.)
            : vec3(vertex_position.x * TileRate0, vertex_position.y, vertex_position.z * TileRate0);
    }
    worldPos = (World * vec4(vertex_position, 1.0)).xyz;
    normal = (NormalMatrix * vec4(vertex_normal,0)).xyz;
    gl_Position = (ViewProjection * World) * vec4(p, 1.0);
}

@fragment
uniform vec4 Dc;
in vec3 normal;
in vec3 worldPos;
out vec4 out_color;

void main()
{
    vec3 norm = normalize(normal);
    vec3 lightDir = normalize(vec3(0.,-150.,0.) - worldPos);  
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 color = (Dc.rgb * diff) * 0.3;
    out_color = vec4(Dc.rgb * 0.7 + color, Dc.a);
}
