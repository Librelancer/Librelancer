// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;

namespace LancerEdit
{
    // Adapted from http://asliceofrendering.com/scene%20helper/2020/01/05/InfiniteGrid/
    public static class GridRender
    {
        private static VertexBuffer vertices;
        private static ElementBuffer elements;
        private static bool loaded = false;

        private static Shader shader;
        private static int gridColor;
        private static int gridScale;
        private static int near;
        private static int far;
        private const string VertexShader = @"#version {0}
layout(std140) uniform Camera_Matrices
{
    mat4 View;
    mat4 Projection;
    mat4 ViewProjection;
};

in vec3 vertex_position;

out vec3 nearPoint;
out vec3 farPoint;
out mat4 fragView;
out mat4 fragProj;

vec3 UnprojectPoint(float x, float y, float z, mat4 view, mat4 projection) {
    mat4 viewInv = inverse(view);
    mat4 projInv = inverse(projection);
    vec4 unprojectedPoint =  viewInv * projInv * vec4(x, y, z, 1.0);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

void main()
{
    vec3 p = vertex_position;
    nearPoint = UnprojectPoint(p.x, p.y, 0.0, View, Projection).xyz; // unprojecting on the near plane
    farPoint = UnprojectPoint(p.x, p.y, 1.0, View, Projection).xyz; // unprojecting on the far plane
    gl_Position = vec4(p, 1.0);
    fragView = View;
    fragProj = Projection;
}
";

        private const string FragmentShader = @"#version {0}
uniform float near;
uniform float far;
uniform float gridScale;
uniform vec4 gridColor;

in vec3 nearPoint;
in vec3 farPoint;
in mat4 fragView;
in mat4 fragProj;
out vec4 outColor;

vec4 grid(vec3 fragPos3D, float scale, bool drawAxis) {
    vec2 coord = fragPos3D.xz * scale;
    vec2 derivative = fwidth(coord);
    vec2 grid = abs(fract(coord - 0.5) - 0.5) / derivative;
    float line = min(grid.x, grid.y);
    float minimumz = min(derivative.y, 1);
    float minimumx = min(derivative.x, 1);
    vec4 color = vec4(gridColor.r, gridColor.g, gridColor.b, 1.0 - min(line, 1.0));
    // z axis
    if(fragPos3D.x > (-0.1 / scale) * minimumx && fragPos3D.x < (0.1 / scale) * minimumx)
        color.rgb = vec3(0.2, 0.2, 1.0);
    // x axis
    if(fragPos3D.z > (-0.1 / scale) * minimumz && fragPos3D.z < (0.1 / scale) * minimumz)
        color.rgb = vec3(1.0, 0.2, 0.2);
    color.a *= gridColor.a;
    return color;
}
float computeDepth(vec3 pos) {
    vec4 clip_space_pos = fragProj * fragView * vec4(pos.xyz, 1.0);
    return clip_space_pos.z / clip_space_pos.w;
}
float computeLinearDepth(vec3 pos) {
    vec4 clip_space_pos = fragProj * fragView * vec4(pos.xyz, 1.0);
    float clip_space_depth = (clip_space_pos.z / clip_space_pos.w) * 2.0 - 1.0; // put back between -1 and 1
    float linearDepth = (2.0 * near * far) / (far + near - clip_space_depth * (far - near)); // get linear value between 0.01 and 100
    return linearDepth / far; // normalize
}
void main() {
    float t = -nearPoint.y / (farPoint.y - nearPoint.y);
    vec3 fragPos3D = nearPoint + t * (farPoint - nearPoint);

    gl_FragDepth = ((gl_DepthRange.diff * computeDepth(fragPos3D)) +
                gl_DepthRange.near + gl_DepthRange.far) / 2.0;

    float linearDepth = computeLinearDepth(fragPos3D);
    float fading = max(0, (0.5 - linearDepth));

    outColor = (grid(fragPos3D, gridScale * 10, true) + grid(fragPos3D, gridScale, true))* float(t > 0); // adding multiple resolution for the grid
    outColor.a *= fading;
}
";
        static void Load(RenderContext context)
        {
            if (loaded) return;
            loaded = true;
            vertices = new VertexBuffer(context, typeof(VertexPosition), 6);
            vertices.SetData<VertexPosition>(new[]
            {
                new VertexPosition(new Vector3(1,1,0)),
                new VertexPosition(new Vector3(-1,-1,0)),
                new VertexPosition(new Vector3(-1,1,0)),
                new VertexPosition(new Vector3(-1,-1,0)),
                new VertexPosition(new Vector3(1,1,0)),
                new VertexPosition(new Vector3(1, -1, 0)),
            });
            string glslVer = context.HasFeature(GraphicsFeature.GLES) ? "310 es\nprecision mediump float;\nprecision mediump int;" : "140";
            shader = new Shader(context, VertexShader.Replace("{0}", glslVer), FragmentShader.Replace("{0}", glslVer));
            near = shader.GetLocation("near");
            far = shader.GetLocation("far");
            gridScale = shader.GetLocation("gridScale");
            gridColor = shader.GetLocation("gridColor");
        }

        public static float DistanceScale(float y)
        {
            float gridScale = 1f;
            if (y >= 15f)
                gridScale = 0.1f;
            if (y >= 60f)
                gridScale = 0.005f;
            if (y >= 200f)
                gridScale = 0.001f;
            if (y >= 9000f)
                gridScale = 0.0001f;
            if (y >= 23000f)
                gridScale = 0.00001f;
            if (y >= 80000f)
                gridScale = 0.000001f;
            return gridScale;
        }

        public static void Draw(RenderContext rstate, float scale, Color4 color, float nearPlane, float farPlane)
        {
            Load(rstate);
            rstate.Cull = false;
            rstate.BlendMode = BlendMode.Normal;
            //Draw
            shader.SetFloat(near, nearPlane);
            shader.SetFloat(far, farPlane);
            shader.SetFloat(gridScale, scale);
            shader.SetColor4(gridColor, color);
            rstate.Shader = shader;
            rstate.DepthWrite = false;
            vertices.Draw(PrimitiveTypes.TriangleList, 2);
            rstate.DepthWrite = true;
            //Restore State
            rstate.BlendMode = BlendMode.Opaque;
            rstate.Cull = true;
        }
    }
}
