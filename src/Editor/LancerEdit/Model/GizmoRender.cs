// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Render;

namespace LancerEdit
{
    static class GizmoRender
    {
        public static void AddGizmoArc(LineRenderer lr, float scale, Matrix4x4 tr, float min, float max)
        {
            //angle magic for display
            float a = -max, b = -min;
            max = b; min = a;

            //Setup
            float baseSize = 0.33f * scale;
            float arrowSize = 0.62f * scale;
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
            lr.DrawPoint(notch_inner, Color4.Yellow);
            lr.DrawPoint(notch_outer, Color4.Yellow);

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
                lr.DrawPoint(p1_inner, Color4.Yellow);
                lr.DrawPoint(p1_outer, Color4.Yellow);
                lr.DrawPoint(p1_outer, Color4.Yellow);
                lr.DrawPoint(p2_outer, Color4.Yellow);
                lr.DrawPoint(p2_outer, Color4.Yellow);
                lr.DrawPoint(p2_inner, Color4.Yellow);
                lr.DrawPoint(p2_inner, Color4.Yellow);
                lr.DrawPoint(p1_inner, Color4.Yellow);
                //Next
                y = y2;
                x = x2;
            }

            //Second notch
            notch_inner = Vector3.Transform(new Vector3((float)x * outerRadius, arrowOffset, -(float)y * outerRadius), tr);
            notch_outer = Vector3.Transform(new Vector3((float)x * notchRadius, arrowOffset, -(float)y * notchRadius), tr);
            lr.DrawPoint(notch_inner, Color4.Yellow);
            lr.DrawPoint(notch_outer, Color4.Yellow);
        }

        static readonly int[] gizmoIndices =
        {
            0,1,2, 0,2,3, 0,3,4, 0,4,1, 1,5,2, 2,5,3, 3,5,4,
            4,5,1, 6,7,8, 6,8,9, 6,9,10, 6,10,7, 7,11,8,
            8,11,9,9,11,10,10,11,7
        };
        static Vector3 vM(float x, float y, float z) => new Vector3(x, z, -y);

        public static void AddGizmo(LineRenderer lr, float scale, Matrix4x4 tr, Color4 color)
        {
            float baseSize = 0.33f * scale;
            float arrowSize = 0.62f * scale;
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
            lr.DrawTriangleMesh(tr, vertices, gizmoIndices, color);
        }

        public const float ScaleFactor = 21.916825f;


    }
}
