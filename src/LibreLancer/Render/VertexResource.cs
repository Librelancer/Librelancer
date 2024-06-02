// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Render
{
    public class VertexResource : IDisposable
    {
        internal VertexResourceAllocator Allocator { get; set; }
        public FVFVertex VertexType { get; init; }
        public VertexBuffer VertexBuffer { get; private set; }
        public int BaseVertex { get; init; }
        public int VertexCount { get; init; }
        public int StartIndex { get; init; }
        public int IndexCount { get; init; }
        public bool IsDisposed { get; private set; }

        public VertexResource(VertexBuffer buffer) =>
            VertexBuffer = buffer;

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            VertexBuffer = null;
            Allocator?.OnFree(this);
        }
    }

    public class IndexResource : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public int StartIndex { get; init; }

        public int IndexCount { get; init; }

        internal VertexResourceAllocator Allocator { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            Allocator?.OnFree(this);
        }
    }

    public class VertexResourceAllocator : IDisposable
    {
        private const int INITIAL_ELEMENT_BUFFER_SIZE = 2 * 1024 * 1024;

        private ElementBuffer elementBuffer;
        private FreeList indexFree = new FreeList();

        private Dictionary<D3DFVF, VertexResourceBuffer> buffersByType = new Dictionary<D3DFVF, VertexResourceBuffer>();
        private List<VertexResource> resources = new List<VertexResource>();
        private List<IndexResource> indexResources = new List<IndexResource>();

        private RenderContext context;

        public VertexResourceAllocator(RenderContext context)
        {
            this.context = context;
            elementBuffer = new ElementBuffer(context, INITIAL_ELEMENT_BUFFER_SIZE);
            indexFree.AddItem(0, INITIAL_ELEMENT_BUFFER_SIZE);
        }

        public VertexResource Allocate(FVFVertex format, byte[] vertices, ushort[] indices)
        {
            if (!indexFree.TryAllocate(indices.Length, out int startIndex))
            {
                FLLog.Debug("Vertices", $"Growing GPU element buffer");
                startIndex = elementBuffer.IndexCount;
                indexFree.AddItem(startIndex + indices.Length, INITIAL_ELEMENT_BUFFER_SIZE - indices.Length);
                elementBuffer.Expand(elementBuffer.IndexCount + INITIAL_ELEMENT_BUFFER_SIZE);
            }
            elementBuffer.SetData(indices, indices.Length, startIndex);
            if (!buffersByType.TryGetValue(format.FVF, out var buffer))
            {
                buffer = new VertexResourceBuffer(context, format, elementBuffer);
                buffersByType[format.FVF] = buffer;
            }
            buffer.Allocate(vertices, out int baseVertex);
            var r = new VertexResource(buffer.VertexBuffer)
            {
                Allocator = this,
                VertexType = format,
                BaseVertex = baseVertex,
                VertexCount = vertices.Length / buffersByType[format.FVF].Stride,
                StartIndex = startIndex,
                IndexCount = indices.Length,
            };
            resources.Add(r);
            return r;
        }

        public IndexResource AllocateIndex(ushort[] indices)
        {
            if (!indexFree.TryAllocate(indices.Length, out int startIndex))
            {
                FLLog.Debug("Vertices", $"Growing GPU element buffer");
                startIndex = elementBuffer.IndexCount;
                indexFree.AddItem(startIndex + indices.Length, INITIAL_ELEMENT_BUFFER_SIZE - indices.Length);
                elementBuffer.Expand(elementBuffer.IndexCount + INITIAL_ELEMENT_BUFFER_SIZE);
            }
            elementBuffer.SetData(indices, indices.Length, startIndex);
            var r = new IndexResource()
            {
                Allocator = this,
                StartIndex = startIndex,
                IndexCount = indices.Length
            };
            indexResources.Add(r);
            return r;
        }

        internal void OnFree(VertexResource resource)
        {
            indexFree.Free(resource.StartIndex, resource.IndexCount);
            buffersByType[resource.VertexType.FVF].Free(resource);
            resources.Remove(resource);
        }

        internal void OnFree(IndexResource resource)
        {
            indexFree.Free(resource.StartIndex, resource.IndexCount);
            indexResources.Remove(resource);
        }

        public void Dispose()
        {
            elementBuffer.Dispose();
            foreach (var r in resources)
            {
                r.Allocator = null;
                r.Dispose();
            }
            resources = null;
            foreach (var i in indexResources)
            {
                i.Allocator = null;
                i.Dispose();
            }
            indexResources = null;
            foreach(var b in buffersByType.Values)
                b.Dispose();
            buffersByType = null;
        }

        class FreeList
        {
            struct FreeItem
            {
                public int Start;
                public int Count;
            }

            private List<FreeItem> list = new List<FreeItem>();

            public void AddItem(int start, int count)
            {
                list.Add(new FreeItem() { Start = start, Count = count });
            }

            public bool TryAllocate(int length, out int start)
            {
                var freeItem = list
                    .Select((x, index) => (x.Start, x.Count, Index: index))
                    .Where(x => x.Count >= length)
                    .DefaultIfEmpty()
                    .MinBy(x => x.Count);
                if (freeItem.Count != 0)
                {
                    if (freeItem.Count > length)
                        list[freeItem.Index] = new FreeItem()
                            {Start = freeItem.Start + length, Count = freeItem.Count - length};
                    else
                        list.RemoveAt(freeItem.Index);
                    start = freeItem.Start;
                    return true;
                }
                start = 0;
                return false;
            }

            public bool Free(int start, int count, int capacity = 0)
            {
                list.Add(new FreeItem() { Start = start, Count = count });
                list.Sort((x, y) => x.Start.CompareTo(y.Start));
                for (int i = 0; i < list.Count; i++) {
                    while (i + 1 < list.Count
                        && list[i + 1].Start == (list[i].Start + list[i].Count))
                    {
                        list[i] = new FreeItem()
                            { Start = list[i].Start, Count = list[i].Count + list[i + 1].Count};
                        list.RemoveAt(i + 1);
                    }
                }

                return list.Count == 1 && list[0].Start == 0 && list[0].Count == capacity;
            }
        }

        class VertexResourceBuffer : IDisposable
        {
            const int VERTEX_BUFSIZE = (int)(8 * 1024 * 1024);

            private FreeList freeList = new FreeList();
            private int resizeCount = 0;
            public VertexBuffer VertexBuffer;

            private ElementBuffer elementBuffer;
            private FVFVertex type;
            private int chunkSize;

            private RenderContext context;

            public int Stride => type.Stride;

            public VertexResourceBuffer(RenderContext context, FVFVertex type, ElementBuffer elementBuffer)
            {
                this.context = context;
                this.type = type;
                this.elementBuffer = elementBuffer;
                chunkSize = VERTEX_BUFSIZE / type.Stride;
            }

            public void Allocate(byte[] vertices, out int baseVertex)
            {
                //
                if (VertexBuffer == null)
                {
                    FLLog.Debug("Vertices", $"Allocating GPU resource for {type}");
                    VertexBuffer = new VertexBuffer(context, type, chunkSize, false);
                    VertexBuffer.SetElementBuffer(elementBuffer);
                    freeList.AddItem(0, chunkSize);
                }
                //
                if (!freeList.TryAllocate(vertices.Length / Stride, out baseVertex)){
                    FLLog.Debug("Vertices", $"Expanding GPU resource for {type}");
                    baseVertex = VertexBuffer.VertexCount;
                    freeList.AddItem(baseVertex + (vertices.Length / type.Stride), chunkSize - (vertices.Length / type.Stride));
                    VertexBuffer.Expand(VertexBuffer.VertexCount + chunkSize);
                }
                VertexBuffer.SetData<byte>(vertices, baseVertex);
            }

            public void Free(VertexResource resource)
            {
                if (freeList.Free(resource.BaseVertex, resource.VertexCount, VertexBuffer.VertexCount))
                {
                    FLLog.Debug("Vertices", $"Deleting GPU resource for {type}, all allocations free");
                    VertexBuffer.Dispose();
                    VertexBuffer = null;
                    freeList = new FreeList();
                }
            }

            public void Dispose()
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
                freeList = new FreeList();
            }
        }
    }
}
