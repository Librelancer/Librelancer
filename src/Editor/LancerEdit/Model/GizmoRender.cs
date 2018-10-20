// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LancerEdit
{
    static class GizmoRender
    {
        public const int MAX_GIZMOS = 400;

        static readonly ushort[] idxcube = new ushort[] {
            0, 1, 2,
            2, 3, 0,
            // top
            1, 5, 6,
            6, 2, 1,
            // back
            7, 6, 5,
            5, 4, 7,
            // bottom
            4, 0, 3,
            3, 7, 4,
            // left
            4, 5, 1,
            1, 0, 4,
            // right
            3, 2, 6,
            6, 7, 3,
        };

        static readonly Vector3[] vertcube = new Vector3[] {
                // front
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3(1.0f, -1.0f,  1.0f),
                new Vector3(1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                // back
                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector3(1.0f, -1.0f, -1.0f),
                new Vector3(1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f),
        };

        public const float LINE_LENGTH = 2.7f;
        public const float CUBE_SIZE = 0.3f;
        public const float ARC_SIZE = 3.24f;
        //Render State
        public static float Scale = 1f;
        public static Color4 CubeColor = Color4.Purple;
        public static float CubeAlpha = 0.3f;

        static Material gizmoMaterial;
        static VertexPositionColor[] lines;
        static VertexPositionColor[] tris;
        static VertexBuffer lineBuffer;
        static VertexBuffer triBuffer;
        static ElementBuffer triElems;
        static int vertexCountC = 0;
        static int vertexCountL = 0;

        static bool inited = false;
        public static void Init(ResourceManager res)
        {
            if (inited) return;
            inited = true;
            lines = new VertexPositionColor[MAX_GIZMOS * 6];
            tris = new VertexPositionColor[MAX_GIZMOS * 8];
            lineBuffer = new VertexBuffer(typeof(VertexPositionColor), MAX_GIZMOS * 6, true);
            ushort[] indices = new ushort[MAX_GIZMOS * idxcube.Length];
            for (int i = 0; i < MAX_GIZMOS; i++)
            {
                for (int j = 0; j < idxcube.Length; j++)
                {
                    indices[i * idxcube.Length + j] = (ushort)(idxcube[j] + (8 * i));
                }
            }
            triBuffer = new VertexBuffer(typeof(VertexPositionColor), MAX_GIZMOS * 8, true);
            triElems = new ElementBuffer(MAX_GIZMOS * idxcube.Length);
            triElems.SetData(indices);
            triBuffer.SetElementBuffer(triElems);

            gizmoMaterial = new Material(res);
            gizmoMaterial.Dc = Color4.White;
            gizmoMaterial.DtName = ResourceManager.WhiteTextureName;
        }


        public static void Begin()
        {
            vertexCountC = vertexCountL = 0;
        }

        //We're working with rather small values here, making this too big
        //messes with floating point.
        const int ARC_SEGMENTS = 16;

        public static void AddGizmoArc(Matrix4 tr, float min, float max)
        {
            var length = max - min;
            AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Yellow);
            var r = ARC_SIZE * Scale;
            var x = r * Math.Cos(min);
            var y = r * Math.Sin(min);
            int segments = ARC_SEGMENTS;
            if (length > Math.PI) segments *= 2; //Double when size of arc is sufficent
            for (int i = 0; i < segments; i++)
            {
                float theta = (length * i) / segments;
                x = r * Math.Cos(min + theta);
                y = r * Math.Sin(min + theta);
                AddPoint(VectorMath.Transform(new Vector3(
                    (float)y,0,-(float)x
                ), tr), Color4.Yellow);
                AddPoint(VectorMath.Transform(new Vector3(
                    (float)y,0,-(float)x
               ), tr), Color4.Yellow);
            }
            AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Yellow);
        }
        public static void AddGizmo(Matrix4 tr, bool cube = true, bool lines = true)
        {
            if (lines)
            {
                //X
                AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Red);
                AddPoint(VectorMath.Transform(Vector3.UnitX * LINE_LENGTH * Scale, tr), Color4.Red);
                //Y
                AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Green);
                AddPoint(VectorMath.Transform(Vector3.UnitY * LINE_LENGTH * Scale, tr), Color4.Green);
                //Z
                AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Blue);
                AddPoint(VectorMath.Transform(-Vector3.UnitZ * LINE_LENGTH * Scale, tr), Color4.Blue);
            }
            //Cube
            if (cube)
                AddCube(ref tr, CubeColor);
        }

        public static void RenderGizmos(ICamera cam, RenderState rstate)
        {
            rstate.DepthEnabled = true;
            triBuffer.SetData(tris, vertexCountC);
            lineBuffer.SetData(lines, vertexCountL);
            gizmoMaterial.Update(cam);
            var r = (BasicMaterial)gizmoMaterial.Render;
            r.World = Matrix4.Identity;
            //Cubes
            r.AlphaEnabled = true;
            rstate.Cull = true;
            r.Use(rstate, lines[0], ref Lighting.Empty);
            triBuffer.Draw(PrimitiveTypes.TriangleList, (idxcube.Length * (vertexCountC / 8)) / 3);
            //Lines
            r.AlphaEnabled = false;
            rstate.Cull = false;
            r.Use(rstate, lines[0], ref Lighting.Empty);
            lineBuffer.Draw(PrimitiveTypes.LineList, vertexCountL / 2);
        }

        static void AddPoint(Vector3 pos, Color4 col)
        {
            lines[vertexCountL++] = new VertexPositionColor(pos, col);
        }

        static void AddCube(ref Matrix4 mat, Color4 col)
        {
            col.A = CubeAlpha;
            foreach (var vert in vertcube)
                tris[vertexCountC++] = new VertexPositionColor(
                    VectorMath.Transform(vert * CUBE_SIZE * Scale, mat),
                    col);
        }
    }
}
