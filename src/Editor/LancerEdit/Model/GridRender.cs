// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.InteropServices;
using LancerEdit.Shaders;
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FragmentVariables
        {
            public Color4 Color;
            public Matrix4x4 ViewProjection;
            public float Near;
            public float Far;
            public float Scale;
        }


        public static void Draw(RenderContext rstate, ICamera camera, float scale, Color4 color, float nearPlane, float farPlane)
        {
            Load(rstate);
            Matrix4x4.Invert(camera.ViewProjection, out var inverseViewProjection);
            var fv = new FragmentVariables();
            fv.Color = color;
            fv.ViewProjection = camera.ViewProjection;
            fv.Near = nearPlane;
            fv.Far = farPlane;
            fv.Scale = scale;
            var shader = EditorShaders.Grid.Get(0);
            shader.SetUniformBlock(0, ref inverseViewProjection);
            shader.SetUniformBlock(3, ref fv);
            var wf = rstate.Wireframe;
            rstate.Wireframe = false;
            rstate.Cull = false;
            rstate.BlendMode = BlendMode.Normal;
            //Draw
            rstate.Shader = shader;
            rstate.DepthWrite = false;
            vertices.Draw(PrimitiveTypes.TriangleList, 2);
            rstate.DepthWrite = true;
            //Restore State
            rstate.BlendMode = BlendMode.Opaque;
            rstate.Cull = true;
            rstate.Wireframe = wf;
        }
    }
}
