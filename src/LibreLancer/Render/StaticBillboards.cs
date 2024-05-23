// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Render
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexBillboardColor2 : IVertexType
    {
        public Vector3 Position;
        public Color4 Color;
        public Color4 Color2;
        public Vector2 TextureCoordinate;
        public Vector3 Dimensions;
        public VertexBillboardColor2(Vector3 position, float x, float y, float angle, Color4 color1, Color4 color2, Vector2 tex)
        {
            Position = position;
            Color = color1;
            Color2 = color2;
            TextureCoordinate = tex;
            Dimensions = new Vector3(x, y, angle);
        }

        public VertexBillboardColor2(Vector3 position, Vector2 tex)
        {
            Position = position;
            Color = Color2 = Color4.White;
            TextureCoordinate = tex;
            Dimensions = Vector3.Zero;
        }

        public VertexDeclaration GetVertexDeclaration()
        {
            return new VertexDeclaration(
                sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float) * 4 + sizeof(float) * 2 + sizeof(float) * 3,
                new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
                new VertexElement(VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 3),
                new VertexElement(VertexSlots.Color2, 4, VertexElementType.Float, false, sizeof(float) * 7),
                new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 11),
                new VertexElement(VertexSlots.Dimensions, 3, VertexElementType.Float, false, sizeof(float) * 13)
            );
        }
    }

    public class StaticBillboards : IDisposable
    {
        public const int MAX_QUADS = 400;
        public VertexBuffer VertexBuffer;
        ElementBuffer ibo;
        struct SunVtx
        {
            public int ID;
            public int IndexStart;
        }
        List<SunVtx> vertexPtrs = new List<SunVtx>();
        int vertexOffset = 0;
        public StaticBillboards(RenderContext rstate)
        {
            VertexBuffer = new VertexBuffer(rstate, typeof(VertexBillboardColor2), MAX_QUADS * 4);
            ibo = new ElementBuffer(rstate, MAX_QUADS * 6);
            var indices = new ushort[MAX_QUADS * 6];
            int iptr = 0;
            for (int i = 0; i < (MAX_QUADS * 4); i += 4)
            {
                /* Triangle 1 */
                indices[iptr++] = (ushort)i;
                indices[iptr++] = (ushort)(i + 1);
                indices[iptr++] = (ushort)(i + 2);
                /* Triangle 2 */
                indices[iptr++] = (ushort)(i + 1);
                indices[iptr++] = (ushort)(i + 3);
                indices[iptr++] = (ushort)(i + 2);
            }
            ibo.SetData(indices);
            VertexBuffer.SetElementBuffer(ibo);
            NebulaFill();
        }

        public const int NebulaFillPrimCount = 6;

        void NebulaFill()
        {
            int a = 0;
            DoVertices(ref a, new VertexBillboardColor2[]
            {
                //X Axis
                new(new Vector3(-1, -1, 0), new Vector2(0, 1)),
                new (new Vector3(+1, -1, 0), new Vector2(1, 1)),
                new(new Vector3(-1, +1, 0), new Vector2(0, 0)),
                new(new Vector3(+1, +1, 0), new Vector2(1, 0)),
                //Z axis
                new(new Vector3(0, -1, -1), new Vector2(0, 1)),
                new(new Vector3(0, -1, +1), new Vector2(1, 1)),
                new(new Vector3(0, +1, -1), new Vector2(0, 0)),
                new(new Vector3(0, +1, +1), new Vector2(0, 1)),
                //Y Axis
                new(new Vector3(-1, 0, -1), new Vector2(0, 1)),
                new(new Vector3(-1, 0, 1), new Vector2(1, 1)),
                new(new Vector3(+1, 0, -1), new Vector2(0, 0)),
                new(new Vector3(+1, 0, 1), new Vector2(0, 1)),
            });
        }

        int idCounter = 0;
        public int DoVertices(ref int id, VertexBillboardColor2[] vertices)
        {
            if (id == 0) id = idCounter++;
            foreach(var v in vertexPtrs)
            {
                if (v.ID == id)
                    return v.IndexStart;
            }
            var idxOffset = (vertexOffset / 4) * 6;
            if (vertexOffset + vertices.Length >= (MAX_QUADS * 4)) return -1;
            VertexBuffer.SetData<VertexBillboardColor2>(vertices.AsSpan(), vertexOffset);
            vertexOffset += vertices.Length;
            var vtx = new SunVtx() { ID = id, IndexStart = idxOffset };
            vertexPtrs.Add(vtx);
            return idxOffset;
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            ibo.Dispose();
        }
    }
}
