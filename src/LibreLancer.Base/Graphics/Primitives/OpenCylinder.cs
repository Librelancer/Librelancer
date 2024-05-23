// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics.Primitives
{
    public class OpenCylinder : IDisposable
    {
        public VertexBuffer VertexBuffer { get; private set; }
        ElementBuffer elemBuffer;
        int primCount;
        VertexPositionNormalTexture[] vertices;
        ushort[] indices;
        ushort[] indices_temp;
        int[] indices_sort;
        public OpenCylinder(RenderContext context, int slices)
        {
            vertices = new VertexPositionNormalTexture[(slices + 2) * 2];
            indices = new ushort[slices * 6];
            indices_temp = new ushort[slices * 6];
            indices_sort = new int[slices * 2];
            primCount = slices * 2;
            float height = 0.5f, radius = 1;
            int iptr = 0;
            int vptr = 0;
            float texAdvance = 1f / slices;
            for (int i = 0; i < slices; i++)
            {
                var normal = GetCircleVector(i, slices);
                //generate vertices
                vertices[vptr++] = new VertexPositionNormalTexture(
                    normal * radius + Vector3.UnitY * height,
                    normal,
                    new Vector2(i, 1)
                );
                vertices[vptr++] = new VertexPositionNormalTexture(
                    normal * radius - Vector3.UnitY * height,
                    normal,
                    new Vector2(i, 0)
                );
                //generate indicess
                indices[iptr++] = (ushort)((i * 2 + 2) % ((slices + 1) * 2));
                indices[iptr++] = (ushort)(i * 2 + 1);
                indices[iptr++] = (ushort)(i * 2);

                indices[iptr++] = (ushort)((i * 2 + 2) % ((slices + 1) * 2));
                indices[iptr++] = (ushort)((i * 2 + 3) % ((slices + 1) * 2));
                indices[iptr++] = (ushort)(i * 2 + 1);
            }
            //last vertex
            var last = GetCircleVector(slices, slices);

            vertices[vptr++] = new VertexPositionNormalTexture(
                last * radius + Vector3.UnitY * height,
                last,
                new Vector2(slices, 1)
            );
            vertices[vptr++] = new VertexPositionNormalTexture(
                last * radius - Vector3.UnitY * height,
                last,
                new Vector2(slices, 0)
            );
            //upload
            VertexBuffer = new VertexBuffer(context, typeof(VertexPositionNormalTexture), vertices.Length);
            VertexBuffer.SetData<VertexPositionNormalTexture>(vertices);
            elemBuffer = new ElementBuffer(context, indices.Length);
            elemBuffer.SetData(indices);
            VertexBuffer.SetElementBuffer(elemBuffer);
        }
        public Vector3 GetSidePosition(int side)
        {
            var v0 = vertices[indices[side * 6]].Position;
            var v1 = vertices[indices[side * 6 + 1]].Position;
            var v2 = vertices[indices[side * 6 + 2]].Position;
            var v3 = vertices[indices[side * 6 + 3]].Position;
            var v4 = vertices[indices[side * 6 + 4]].Position;
            var v5 = vertices[indices[side * 6 + 5]].Position;
            return (v0 + v1 + v2 + v3 + v4 + v5) / 6f;
        }
        public int PrimitiveCount
        {
            get
            {
                return primCount;
            }
        }
        static Vector3 GetCircleVector(int i, int slices)
        {
            float angle = i * MathHelper.TwoPi / slices;
            float dx = (float)Math.Cos(angle);
            float dz = (float)Math.Sin(angle);
            return new Vector3(dx, 0, dz);
        }
        public void Dispose()
        {
            VertexBuffer.Dispose();
            elemBuffer.Dispose();
        }
    }
}
