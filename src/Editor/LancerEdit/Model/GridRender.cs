// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer;
using LibreLancer.Render;
using LibreLancer.Vertices;

namespace LancerEdit
{
    public static class GridRender
    {
        private static VertexBuffer vertices;
        private static ElementBuffer elements;


        private const float GRID_SIZE = 1024;
        private const float TEXCOORD = 1024;
        private static bool loaded = false;

        private static Shader shader;
        private static int gridColor;
        private static int gridScale;
        private const string VertexShader = @"#version {0}
layout(std140) uniform Camera_Matrices 
{
    mat4 View;
    mat4 Projection;
    mat4 ViewProjection;
};

uniform float GridScale;
in vec3 vertex_position;
in vec2 vertex_texture1;
out vec2 texcoord;
out vec4 view_position;
void main()
{
    gl_Position = ViewProjection * vec4(vertex_position, 1);
    texcoord = vertex_texture1 / GridScale;
    view_position = View * vec4(vertex_position, 1);
}
";

        private const string FragmentShader = @"#version {0}
in vec2 texcoord;
in vec4 view_position;
out vec4 out_color;

const float FADE_NEAR = 10.0;
const float FADE_FAR = 100.0;
const float N = 25.0; //gives a decent thickness

uniform vec4 GridColor;
float invGridAlpha( in vec2 p, in vec2 ddx, in vec2 ddy )
{
    vec2 w = max(abs(ddx), abs(ddy)) + 0.01;
    vec2 a = p + 0.5*w;                        
    vec2 b = p - 0.5*w;           
    vec2 i = (floor(a)+min(fract(a)*N,1.0)-
              floor(b)-min(fract(b)*N,1.0))/(N*w);
    return (1.0-i.x)*(1.0-i.y);
}

void main()
{
    float grid = (1.0 - invGridAlpha(texcoord, dFdx(texcoord), dFdy(texcoord)));
    float dist = length(view_position);
	float fadeFactor = (FADE_FAR- dist) / (FADE_FAR - FADE_NEAR);
	fadeFactor = clamp(fadeFactor, 0.0, 1.0);
    out_color = GridColor * vec4(1.0,1.0,1.0,grid * fadeFactor); 
}
";
        static void Load()
        {
            if (loaded) return;
            loaded = true;
            vertices = new VertexBuffer(typeof(VertexPositionTexture), 4);
            vertices.SetData(new[]
            {
                new VertexPositionTexture(new Vector3(-GRID_SIZE,0,-GRID_SIZE), Vector2.Zero),
                new VertexPositionTexture(new Vector3(GRID_SIZE,0,-GRID_SIZE),new Vector2(TEXCOORD,0) ),
                new VertexPositionTexture(new Vector3(-GRID_SIZE,0,GRID_SIZE), new Vector2(0,TEXCOORD) ), 
                new VertexPositionTexture(new Vector3(GRID_SIZE,0,GRID_SIZE), new Vector2(TEXCOORD,TEXCOORD) ),
            });
            elements = new ElementBuffer(6);
            elements.SetData(new short[] { 0, 1, 2, 1, 3, 2});
            vertices.SetElementBuffer(elements);
            string glslVer = RenderContext.GLES ? "310 es\nprecision mediump float;\nprecision mediump int;" : "140";
            shader = new Shader(VertexShader.Replace("{0}", glslVer), FragmentShader.Replace("{0}", glslVer));
            gridColor = shader.GetLocation("GridColor");
            gridScale = shader.GetLocation("GridScale");
        }    
        
        public static void Draw(RenderContext rstate, Color4 color)
        {
            Load();
            //Set state
            rstate.Cull = false;
            rstate.DepthEnabled = false;
            rstate.BlendMode = BlendMode.Normal;
            shader.SetFloat(gridScale, 1);
            shader.SetColor4(gridColor, color);
            //Draw
            vertices.Draw(PrimitiveTypes.TriangleList, 2);
            //Restore State
            rstate.BlendMode = BlendMode.Opaque;
            rstate.Cull = true;
            rstate.DepthEnabled = true;
        }
    }
}