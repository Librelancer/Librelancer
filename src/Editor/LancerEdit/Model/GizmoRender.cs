// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LancerEdit
{
    static class GizmoRender
    {
        public const int MAX_LINES = 40000;
        public static float Scale = 1;
        //Render State

        static Material gizmoMaterial;
        static VertexPositionColor[] lines;
        static VertexBuffer lineBuffer;
        static int vertexCountL = 0;

        static bool inited = false;
        public static void Init(ResourceManager res)
        {
            if (inited) return;
            inited = true;
            lines = new VertexPositionColor[MAX_LINES * 2];
            lineBuffer = new VertexBuffer(typeof(VertexPositionColor), MAX_LINES * 2, true);

            gizmoMaterial = new Material(res);
            gizmoMaterial.Dc = Color4.White;
            gizmoMaterial.DtName = ResourceManager.WhiteTextureName;
        }


        public static void Begin()
        {
            vertexCountL = 0;
        }

        public static void AddGizmoArc(Matrix4x4 tr, float min, float max)
        {
            //angle magic for display
            float a = -max, b = -min;
            max = b; min = a;

            //Setup
            float baseSize = 0.33f * Scale;
            float arrowSize = 0.62f * Scale;
            float arrowLength = arrowSize * 3;
            float arrowOffset = (baseSize > 0) ? baseSize + arrowSize : 0;

            var length = max - min;
            var width = arrowLength * 0.125f;
            var radius = arrowLength + width * 2f;
            var innerRadius = radius - width * 0.5f;
            var outerRadius = radius + width * 0.5f;

            var notchRadius = outerRadius + width;
           
            var x = Math.Sin(min);
            var y = Math.Cos(min);

            //First Notch
            var notch_inner = Vector3.Transform(new Vector3((float)x * outerRadius, arrowOffset, -(float)y * outerRadius), tr);
            var notch_outer = Vector3.Transform(new Vector3((float)x * notchRadius, arrowOffset, -(float)y * notchRadius), tr);
            AddPoint(notch_inner, Color4.Yellow);
            AddPoint(notch_outer, Color4.Yellow);

            int segments = (int)Math.Ceiling(length / MathHelper.DegreesToRadians(11.25f));
            if (segments <= 1) segments = 2;
            for (int i = 1; i < segments; i++)
            {
                float theta = (length * i) / segments;
                var x2 = Math.Sin(min + theta);
                var y2 = Math.Cos(min + theta);
                var p1_inner = Vector3.Transform(new Vector3((float)x * innerRadius, arrowOffset, -(float)y * innerRadius), tr);
                var p1_outer = Vector3.Transform(new Vector3((float)x * outerRadius, arrowOffset, -(float)y * outerRadius), tr);
                var p2_inner = Vector3.Transform(new Vector3((float)x2 * innerRadius, arrowOffset, -(float)y2 * innerRadius), tr);
                var p2_outer = Vector3.Transform(new Vector3((float)x2 * outerRadius, arrowOffset, -(float)y2 * outerRadius), tr);
                //Draw quad
                AddPoint(p1_inner, Color4.Yellow);
                AddPoint(p1_outer, Color4.Yellow);
                AddPoint(p1_outer, Color4.Yellow);
                AddPoint(p2_outer, Color4.Yellow);
                AddPoint(p2_outer, Color4.Yellow);
                AddPoint(p2_inner, Color4.Yellow);
                AddPoint(p2_inner, Color4.Yellow);
                AddPoint(p1_inner, Color4.Yellow);
                //Next
                y = y2;
                x = x2;
            }

            //Second notch
            notch_inner = Vector3.Transform(new Vector3((float)x * outerRadius, arrowOffset, -(float)y * outerRadius), tr);
            notch_outer = Vector3.Transform(new Vector3((float)x * notchRadius, arrowOffset, -(float)y * notchRadius), tr);
            AddPoint(notch_inner, Color4.Yellow);
            AddPoint(notch_outer, Color4.Yellow);
        }

        static readonly int[] gizmoIndices =
        {
            0,1,2, 0,2,3, 0,3,4, 0,4,1, 1,5,2, 2,5,3, 3,5,4, 
            4,5,1, 6,7,8, 6,8,9, 6,9,10, 6,10,7, 7,11,8,
            8,11,9,9,11,10,10,11,7
        };
        static Vector3 vM(float x, float y, float z) => new Vector3(x, z, -y);

        public static void AddGizmo(Matrix4x4 tr, Color4 color)
        {
            float baseSize = 0.33f * Scale;
            float arrowSize = 0.62f * Scale;
            float arrowLength = arrowSize * 3;
            float arrowOffset = (baseSize > 0) ? baseSize + arrowSize : 0;
            var vertices = new Vector3[]
            {
                vM(0,0, baseSize), vM(-baseSize, -baseSize, 0),
                vM(baseSize, -baseSize, 0), vM(baseSize, baseSize, 0),
                vM(-baseSize, baseSize, 0), vM(0,0,0),
                vM(0, arrowLength, arrowOffset), vM(-arrowSize, 0, arrowOffset),
                vM(0, 0, arrowOffset + arrowSize), vM(arrowSize, 0, arrowOffset),
                vM(0, 0, arrowOffset - arrowSize), vM(0, -arrowSize, arrowOffset)
            };
            AddTriangleMesh(tr, vertices, gizmoIndices, color);
        }


        public static void RenderGizmos(ICamera cam, RenderContext rstate)
        {
            rstate.DepthEnabled = true;
            lineBuffer.SetData(lines, vertexCountL);
            gizmoMaterial.Update(cam);
            var r = (BasicMaterial)gizmoMaterial.Render;
            //Lines
            r.AlphaEnabled = false;
            rstate.Cull = false;
            r.Use(rstate, lines[0], ref Lighting.Empty);
            lineBuffer.Draw(PrimitiveTypes.LineList, vertexCountL / 2);
        }

        static void AddTriangleMesh(Matrix4x4 mat, Vector3[] positions, int[] indices, Color4 color)
        {
            for(int i = 1; i < indices.Length; i++)
            {
                var p1 = positions[indices[i - 1]];
                var p2 = positions[indices[i]];
                AddPoint(Vector3.Transform(p1, mat), color);
                AddPoint(Vector3.Transform(p2, mat), color);
            }
        }

        static void AddPoint(Vector3 pos, Color4 col)
        {
            lines[vertexCountL++] = new VertexPositionColor(pos, col);
        }
    }
}
