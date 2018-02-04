/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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

        public static float LineScale = 2.7f;
        public static float CubeScale = 0.3f;

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

        public static void AddGizmo(Matrix4 tr, bool cube = true)
        {
            AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Red);
            AddPoint(VectorMath.Transform(Vector3.UnitX * LineScale, tr), Color4.Red);
            //Y
            AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Green);
            AddPoint(VectorMath.Transform(Vector3.UnitY * LineScale, tr), Color4.Green);
            //Z
            AddPoint(VectorMath.Transform(Vector3.Zero, tr), Color4.Blue);
            AddPoint(VectorMath.Transform(Vector3.UnitZ * LineScale, tr), Color4.Blue);
            //Cube
            if (cube)
                AddCube(ref tr, Color4.Purple);
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
            col.A = 0.3f;
            foreach (var vert in vertcube)
                tris[vertexCountC++] = new VertexPositionColor(
                    VectorMath.Transform(vert * CubeScale, mat),
                    col);
        }
    }
}
