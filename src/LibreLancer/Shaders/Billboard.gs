#version 150

layout (points) in;
layout (triangle_strip) out;
layout (max_vertices = 4) out;

in Vertex
{
	vec4 color;
	vec2 size;
	vec2 texture0;
	vec2 texture1;
	vec2 texture2;
	vec2 texture3;
	float angle;
} vertex[];

uniform mat4 ViewProjection;
uniform mat4 View;

out vec2 Vertex_UV;
out vec4 Vertex_Color;


void main(void)
{
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
	//TODO: Non-square is broken
	vec3 sz = vertex[0].size.xxx;

	//Rotation
	float s = sin(vertex[0].angle);
	float c = cos(vertex[0].angle);
	vec3 up = c * srcRight - s * srcUp;
	vec3 right = s * srcRight + c * srcUp;

	vec3 P = gl_in[0].gl_Position.xyz;
 	// a: bottom-left
 	vec3 va = P - (right + up) * sz;
 	gl_Position = ViewProjection * vec4(va, 1);
 	Vertex_UV = vertex[0].texture2;
 	Vertex_Color = vertex[0].color;
 	EmitVertex();  
  
  	// b: top-left
  	vec3 vb = P - (right - up) * sz;
  	gl_Position = ViewProjection * vec4(vb, 1);
  	Vertex_UV = vertex[0].texture0;
  	Vertex_Color = vertex[0].color;
  	EmitVertex();  
  
  	// d: bottom-right
  	vec3 vd = P + (right - up) * sz;
  	gl_Position = ViewProjection * vec4(vd, 1);
  	Vertex_UV = vertex[0].texture3;
  	Vertex_Color = vertex[0].color;
  	EmitVertex();  
 
  	// c: top-right
  	vec3 vc = P + (right + up) * sz;
  	gl_Position = ViewProjection * vec4(vc, 1);
  	Vertex_UV = vertex[0].texture1;
  	Vertex_Color = vertex[0].color;
  	EmitVertex();  
 
  	EndPrimitive();
}