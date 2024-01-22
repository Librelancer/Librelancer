using System;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Utf.Mat;

namespace LancerEdit;

public class NormalLinesMaterial : RenderMaterial
{
    private const string VERTEX = @"
layout(std140) uniform Camera_Matrices
{
    mat4 View;
    mat4 Projection;
    mat4 ViewProjection;
};
in vec3 vertex_position;
in vec3 vertex_normal;

uniform float Size;
uniform mat4x4 World;

out vec4 position1;
out vec3 color1;
out vec4 position2;
out vec3 color2;

void main() {
    position1 = (ViewProjection * World) * vec4(vertex_position, 1.);
    color1 = abs(clamp(vertex_normal, vec3(-1.), vec3(0.)));
    position2 = (ViewProjection * World) * vec4(vertex_position + Size * vertex_normal, 1.);
    color2 = clamp(vertex_normal, vec3(0.), vec3(1.));
}
";
    private const string GEOMETRY = @"
layout (triangles) in;
layout (line_strip, max_vertices = 6) out;

in vec4 position1[];
in vec4 position2[];
in vec3 color1[];
in vec3 color2[];

out vec3 vertex_color;

void main() {
    gl_Position = position1[0];
    vertex_color = color1[0];
    EmitVertex();
    gl_Position = position2[0];
    vertex_color = color2[0];
    EmitVertex();
    EndPrimitive();

    gl_Position = position1[1];
    vertex_color = color1[1];
    EmitVertex();
    gl_Position = position2[1];
    vertex_color = color2[1];
    EmitVertex();
    EndPrimitive();

    gl_Position = position1[2];
    vertex_color = color1[2];
    EmitVertex();
    gl_Position = position2[2];
    vertex_color = color2[2];
    EmitVertex();
    EndPrimitive();
}
";

    private const string FRAGMENT = @"
in vec3 vertex_color;
out vec4 outColor;
void main() {
    outColor = vec4(vertex_color, 1.);
}
";

    private static Shader shader;
    private static int worldLoc;
    private static int sizeLoc;

    static void InitShader(RenderContext context)
    {
        if (shader == null)
        {
            string prelude = "";
            if (context.HasFeature(GraphicsFeature.GLES))
                prelude = "#version 310 es\nprecision highp float;\nprecision highp int;\n";
            else
                prelude = "#version 150\n";
            shader = new Shader(context, prelude + VERTEX, prelude + FRAGMENT, prelude + GEOMETRY);
            worldLoc = shader.GetLocation("World");
            sizeLoc = shader.GetLocation("Size");
        }
    }

    public NormalLinesMaterial(ResourceManager library) : base(library)
    {
    }

    public float Size { get; set; } = 2;

    public override unsafe void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        InitShader(rstate);
        shader.SetMatrix(worldLoc, (IntPtr) World.Source);
        shader.SetFloat(sizeLoc, Size);
        rstate.Shader = shader;
    }

    public override bool IsTransparent => false;

    public override void ApplyDepthPrepass(RenderContext rstate) { }

    public static Material GetMaterial(ResourceManager res, float size)
        => new(new NormalLinesMaterial(res)
        {
            Size = size,
        });
}
