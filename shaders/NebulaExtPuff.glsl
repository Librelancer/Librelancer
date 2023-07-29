@vertex
@include (includes/camera.inc)
in vec3 vertex_position;
in vec3 vertex_dimensions;
in vec4 vertex_color;
in vec4 vertex_color2;

in vec2 vertex_texture1;

out vec2 uv;
out vec4 innercolor;
out vec4 outercolor;

uniform mat4 World;


void main()
{
    //Up/right
    vec3 srcRight = vec3(
        View[0][0],
        View[1][0],
        View[2][0]
    );
    vec3 srcUp = vec3(
        View[0][1],
        View[1][1],
        View[2][1]
    );
    vec3 worldpos = (World * vec4(vertex_position, 1)).xyz;
    //Billboard calculation
    float s = sin(vertex_dimensions.z);
    float c = cos(vertex_dimensions.z);
    vec3 up = c * srcRight - s * srcUp;
    vec3 right = s * srcRight + c * srcUp;
    vec3 pos = worldpos + (right * vertex_dimensions.x) + (up * vertex_dimensions.y);
    gl_Position = ViewProjection * vec4(pos, 1);
    //pass-through to fragment
    uv = vertex_texture1;
    innercolor = vertex_color;
    outercolor = vertex_color2;
}

@fragment
uniform sampler2D DtSampler;
uniform vec4 FogColor;
uniform float FogFactor;
in vec2 uv;
in vec4 innercolor;
in vec4 outercolor;
out vec4 FragColor;

void main(void)
{
	vec4 result = texture(DtSampler, uv) * innercolor;
	result.rgb = mix(result.rgb, outercolor.rgb, FogFactor);
	FragColor = result;
}


