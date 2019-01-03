// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Vertices;
namespace LibreLancer
{
    public class VertexResource<T> where T : struct
    {
        List<VertexResourceBuffer<T>> buffers = new List<VertexResourceBuffer<T>>();
        public void Allocate(T[] vertices, ushort[] indices, out VertexBuffer vbo, out int startIndex, out int baseVertex, out IndexResourceHandle index)
        {
            foreach(var buf in buffers) {
                if(buf.Allocate(vertices,indices, out startIndex, out baseVertex, out index)) {
                    vbo = buf.Buffer;
                    return;
                }
            }
            FLLog.Debug("Vertices", "Allocating 16MiB for " + typeof(T).Name);
            buffers.Add(new VertexResourceBuffer<T>());
            buffers[buffers.Count - 1].Allocate(vertices, indices, out startIndex, out baseVertex, out index);
            vbo = buffers[buffers.Count - 1].Buffer;
        }

    }

    public class IndexResourceHandle
    {
        public ElementBuffer Elements;
        public int CountIndex;
        public int TotalIndex;
    }

    public class VertexResourceBuffer<T> where T : struct
    {
        const int VERTEX_BUFSIZE = (int)(12.75 * 1024 * 1024);
        const int INDEX_BUFSIZE = (int)(3.25 * 1024 * 1024);
        public int TotalVertex;

        public int TotalIndex {  get { return Index.TotalIndex; } set { Index.TotalIndex = value; } }
        public int CountIndex { get { return Index.CountIndex; } set { Index.CountIndex = value; } }
        public ElementBuffer Elements {  get { return Index.Elements; } set { Index.Elements = value; } }

        public VertexBuffer Buffer;
        public int CountVertex;
        public IndexResourceHandle Index = new IndexResourceHandle();
        public VertexResourceBuffer()
        {
            var ivert = (IVertexType)Activator.CreateInstance<T>();
            var decl = ivert.GetVertexDeclaration();
            TotalVertex = VERTEX_BUFSIZE / decl.Stride;
            TotalIndex = INDEX_BUFSIZE / 2;
            Elements = new ElementBuffer(TotalIndex);
            Buffer = new VertexBuffer(typeof(T), TotalVertex);
            Buffer.SetElementBuffer(Elements);
        }

        public bool Allocate(T[] vertices, ushort[] indices, out int startIndex, out int baseVertex, out IndexResourceHandle index)
        {
            index = null;
            startIndex = baseVertex = -1;
            if (CountVertex + vertices.Length > TotalVertex) return false;
            if (CountIndex + indices.Length > TotalIndex) return false;
            index = Index;
            startIndex = CountIndex;
            baseVertex = CountVertex;
            Buffer.SetData(vertices, vertices.Length, CountVertex);
            Elements.SetData(indices, indices.Length, CountIndex);
            CountVertex += vertices.Length;
            CountIndex += indices.Length;
            return true;
        }

        public void Dispose()
        {
            Elements.Dispose();
            Buffer.Dispose();
        }
    }
}
