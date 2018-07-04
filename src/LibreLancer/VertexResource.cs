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
using System.Collections.Generic;
using LibreLancer.Vertices;
namespace LibreLancer
{
    public class VertexResource<T> where T : struct
    {
        List<VertexResourceBuffer<T>> buffers = new List<VertexResourceBuffer<T>>();
        public void Allocate(T[] vertices, ushort[] indices, out VertexBuffer vbo, out int startIndex, out int baseVertex)
        {
            foreach(var buf in buffers) {
                if(buf.Allocate(vertices,indices, out startIndex, out baseVertex)) {
                    vbo = buf.Buffer;
                    return;
                }
            }
            FLLog.Debug("Vertices", "Allocating 6MiB for " + typeof(T).Name);
            buffers.Add(new VertexResourceBuffer<T>());
            buffers[buffers.Count - 1].Allocate(vertices, indices, out startIndex, out baseVertex);
            vbo = buffers[buffers.Count - 1].Buffer;
        }

    }

    public class VertexResourceBuffer<T> where T : struct
    {
        const int VERTEX_BUFSIZE = (int)(4.5 * 1024 * 1024);
        const int INDEX_BUFSIZE = (int)(1.5 * 1024 * 1024);
        public int TotalVertex;
        public int TotalIndex;

        public VertexBuffer Buffer;
        public ElementBuffer Elements;
        public int CountIndex;
        public int CountVertex;

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

        public bool Allocate(T[] vertices, ushort[] indices, out int startIndex, out int baseVertex)
        {
            startIndex = baseVertex = -1;
            if (CountVertex + vertices.Length > TotalVertex) return false;
            if (CountIndex + indices.Length > TotalIndex) return false;
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
