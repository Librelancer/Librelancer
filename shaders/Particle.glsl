@include (includes/sprite.inc)
@vertex
in vec3 vertex_position;
in vec3 vertex_dimensions;
in vec3 vertex_right;
in vec3 vertex_up;
in vec4 vertex_color;
in vec4 vertex_texture1;

out vec2 Vertex_UV;
out vec4 Vertex_Color;

out Vertex
{
    vec3 up;
    vec3 right;
    vec2 size;
    vec4 color;
    vec4 tex;
} vertex;

void main()
{
    //Billboard calculation
    float s = sin(vertex_dimensions.z);
    float c = cos(vertex_dimensions.z);
    vertex.up = c * vertex_right - s * vertex_up;
    vertex.right = s * vertex_right + c * vertex_up;
    gl_Position = vec4(vertex_position, 1);
    //pass-through to geometry
    vertex.tex = vertex_texture1;
    vertex.color = vertex_color;
    vertex.size = vertex_dimensions.xy;
}

@geometry
@include (includes/camera.inc)
layout (points) in;
layout (triangle_strip) out;
layout (max_vertices = 4) out;

in Vertex
{
    vec3 up;
    vec3 right;
    vec2 size;
    vec4 color;
    vec4 tex;
} vertex[];

out vec2 Vertex_UV;
out vec4 Vertex_Color;

uniform bool FlipU;
uniform bool FlipV;

#define RIGHT (vertex[0].right)
#define UP (vertex[0].up)
#define SIZE (vertex[0].size)

void main(void)
{
	vec3 P = gl_in[0].gl_Position.xyz;
	vec4 tex = vertex[0].tex;
	
	float top = FlipV ? tex.y + tex.w : tex.y;
	float bottom = FlipV ? tex.y : tex.y + tex.w;
	float left = FlipU ? tex.x + tex.z : tex.x;
	float right = FlipU ? tex.x : tex.x + tex.z;
	
 	//bottom-left
 	vec3 va = P + (RIGHT * -0.5 * SIZE.x) + (UP * -0.5 * SIZE.y);
 	gl_Position = ViewProjection * vec4(va, 1);
 	Vertex_UV = vec2(left, bottom);
 	Vertex_Color = vertex[0].color;
 	EmitVertex();  
  
  	//bottom-right
  	vec3 vb = P + (RIGHT * 0.5 * SIZE.x) + (UP * -0.5 * SIZE.y);
  	gl_Position = ViewProjection * vec4(vb, 1);
  	Vertex_UV = vec2(right, bottom);
  	Vertex_Color = vertex[0].color;
  	EmitVertex();  
  
  	//top-left
  	vec3 vd = P + (RIGHT * -0.5 * SIZE.x) + (UP * 0.5 * SIZE.y);
  	gl_Position = ViewProjection * vec4(vd, 1);
  	Vertex_UV = vec2(left, top);
  	Vertex_Color = vertex[0].color;
  	EmitVertex();  
 
  	//top-right
  	vec3 vc = P + (RIGHT * 0.5 * SIZE.x) + (UP * 0.5 * SIZE.y);
  	gl_Position = ViewProjection * vec4(vc, 1);
  	Vertex_UV = vec2(right, top);
  	Vertex_Color = vertex[0].color;
  	EmitVertex();  
 
  	EndPrimitive();
}


