// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
namespace LibreLancer.Utf.Vms
{
    public class VMeshData
    {
        public FVFVertex VertexFormat { get; private set; }
        public ushort VertexCount { get; private set; }

        /// <summary>
        /// A list of meshes in the mesh data
        /// </summary>
        public TMeshHeader[] Meshes { get; private set; }

        public byte[] VertexBuffer;
		public ushort[] Indices;

        /// <summary>
        /// A list of triangles in the mesh data
        /// </summary>

        public VMeshResource Resource;

        T Read<T>(int offset, int index)
            where T : unmanaged
        {
            var sz = Unsafe.SizeOf<T>();
            var slice = VertexBuffer.AsSpan().Slice(VertexFormat.Stride * index + offset, sz);
            return MemoryMarshal.Cast<byte, T>(slice)[0];
        }

        public Vector3 GetPosition(int index) => Read<Vector3>(0, index);

        public Vector3 GetNormal(int index)
        {
            if (!VertexFormat.Normal) throw new InvalidOperationException();
            return Read<Vector3>(12, index);
        }

        public VertexDiffuse GetDiffuse(int index)
        {
            if (!VertexFormat.Diffuse) throw new InvalidOperationException();
            return Read<VertexDiffuse>(VertexFormat.Normal ? 24 : 12, index);
        }

        public Vector2 GetTexCoord(int index, int coordinate)
        {
            if (coordinate > VertexFormat.TexCoords) throw new InvalidOperationException();

            int offset = (coordinate * 8) + 12;
            if (VertexFormat.Normal) offset += 12;
            if (VertexFormat.Diffuse) offset += 4;

            return Read<Vector2>( offset, index);
        }

		public VMeshData(ArraySegment<byte> data, string name)
        {
            if (data == null) throw new ArgumentNullException("data");

            using (BinaryReader reader = new BinaryReader(data.GetReadStream()))
            {

                // Read the data header.
                reader.Skip(8); //MeshType = 0x1, SurfaceType = 0x4
                var meshCount = reader.ReadUInt16();
                var indexCount = reader.ReadUInt16();
                VertexFormat = new FVFVertex((D3DFVF)reader.ReadUInt16());
                VertexCount = reader.ReadUInt16();

                // Read the mesh headers.
                Meshes = new TMeshHeader[meshCount];
                int triangleStartOffset = 0;
                for (int count = 0; count < Meshes.Length; count++)
                {
                    TMeshHeader item = new TMeshHeader(reader, triangleStartOffset);
                    if (item.NumRefVertices < 3) {
                        FLLog.Warning("Vms", $"{name} mesh {count} references 0 triangles");
                    }
                    triangleStartOffset += item.NumRefVertices;
                    Meshes[count] = (item);
                }

                // Read the triangle data
                Indices = new ushort[indexCount];
                for (int i = 0; i < indexCount; i++) Indices[i] = reader.ReadUInt16();
                VertexBuffer = reader.ReadBytes(VertexCount * VertexFormat.Stride);
            }
        }

		public void Initialize(ResourceManager cache)
		{
            if (Resource != null && !Resource.IsDisposed)
			{
				return;
			}
            GenerateVertexBuffer(cache);
		}

        void SetResource(VertexResource res)
        {
            Resource = new VMeshResource()
            {
                VertexResource = res,
                Meshes = Meshes,
                Indices = Indices,
            };
        }
        void GenerateVertexBuffer(ResourceManager cache)
        {
            SetResource(cache.AllocateVertices(VertexFormat, VertexBuffer, Indices));
        }

        public override string ToString()
        {
            return VertexFormat.FVF.ToString();
        }
    }
}
