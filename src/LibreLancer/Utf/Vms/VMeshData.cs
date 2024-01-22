// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
namespace LibreLancer.Utf.Vms
{
    public class VMeshData
    {
        public D3DFVF FlexibleVertexFormat { get; private set; } //FVF used for rendering
        public D3DFVF OriginalFVF { get; private set; } //FVF stored in the file
        public ushort VertexCount { get; private set; }

        /// <summary>
        /// A list of meshes in the mesh data
        /// </summary>
        public TMeshHeader[] Meshes { get; private set; }

		public ushort[] Indices;

        /// <summary>
        /// A list of triangles in the mesh data
        /// </summary>

        public VMeshResource Resource;

        public VertexPosition[] verticesVertexPosition { get; private set; }
        public VertexPositionNormal[] verticesVertexPositionNormal { get; private set; }
        public VertexPositionTexture[] verticesVertexPositionTexture { get; private set; }
        public VertexPositionNormalTexture[] verticesVertexPositionNormalTexture { get; private set; }
        public VertexPositionNormalDiffuseTexture[] verticesVertexPositionNormalDiffuseTexture { get; private set; }
        public VertexPositionNormalTextureTwo[] verticesVertexPositionNormalTextureTwo { get; private set; }
        public VertexPositionNormalDiffuseTextureTwo[] verticesVertexPositionNormalDiffuseTextureTwo { get; private set; }

		public VMeshData(ArraySegment<byte> data, string name)
        {
            if (data == null) throw new ArgumentNullException("data");

            using (BinaryReader reader = new BinaryReader(data.GetReadStream()))
            {

                // Read the data header.
                reader.Skip(8); //MeshType = 0x1, SurfaceType = 0x4
                var meshCount = reader.ReadUInt16();
                var indexCount = reader.ReadUInt16();
                FlexibleVertexFormat = (D3DFVF)reader.ReadUInt16();
                OriginalFVF = FlexibleVertexFormat;
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

                // Read the vertex data.
                // The FVF defines what fields are included for each vertex.
                switch (FlexibleVertexFormat)
                {
                    case D3DFVF.XYZ: //(D3DFVF)0x0002:
                        verticesVertexPosition = new VertexPosition[VertexCount];
                        for (int i = 0; i < VertexCount; i++) verticesVertexPosition[i] = new VertexPosition(reader);
                        break;
                    case D3DFVF.XYZ | D3DFVF.NORMAL: //(D3DFVF)0x0012:
                        verticesVertexPositionNormal = new VertexPositionNormal[VertexCount];
                        for (int i = 0; i < VertexCount; i++) verticesVertexPositionNormal[i] = new VertexPositionNormal(reader);
                        break;
                    case D3DFVF.XYZ | D3DFVF.TEX1: //(D3DFVF)0x0102:
                        verticesVertexPositionNormalTexture = new VertexPositionNormalTexture[VertexCount];
                        for (int i = 0; i < VertexCount; i++)
                        {
                            Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
							Vector2 textureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
                            verticesVertexPositionNormalTexture[i] = new VertexPositionNormalTexture(position, Vector3.One, textureCoordinate);
                        }
						FlexibleVertexFormat |= D3DFVF.NORMAL;
                        break;
                    case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX1: //(D3DFVF)0x0112:
                    case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX4: //(D3DFVF)0x0412: (Tangent binormal - ignore)
                        verticesVertexPositionNormalTexture = new VertexPositionNormalTexture[VertexCount];
                        for (int i = 0; i < VertexCount; i++)
                        {
                            Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            Vector3 normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
							Vector2 textureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
                            if ((FlexibleVertexFormat & D3DFVF.TEX4) == D3DFVF.TEX4)
                                reader.BaseStream.Seek(6 * sizeof(float), SeekOrigin.Begin);
                            verticesVertexPositionNormalTexture[i] = new VertexPositionNormalTexture(position, normal, textureCoordinate);
                        }
                        FlexibleVertexFormat = D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX1;
                        break;
                    case D3DFVF.XYZ | D3DFVF.DIFFUSE | D3DFVF.TEX1: //(D3DFVF)0x0142:
                        verticesVertexPositionNormalDiffuseTexture = new VertexPositionNormalDiffuseTexture[VertexCount];
                        for (int i = 0; i < VertexCount; i++)
                        {
                            Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            var diffuse = reader.ReadUInt32();
							Vector2 textureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
                            verticesVertexPositionNormalDiffuseTexture[i] = new VertexPositionNormalDiffuseTexture(position, Vector3.One, diffuse, textureCoordinate);
                        }
						FlexibleVertexFormat |= D3DFVF.NORMAL;
                        break;
                    case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX1: //(D3DFVF)0x0152:
                        verticesVertexPositionNormalDiffuseTexture = new VertexPositionNormalDiffuseTexture[VertexCount];
						for (int i = 0; i < VertexCount; i++)
						{
							var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
							var normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            var diffuse = reader.ReadUInt32();
							Vector2 textureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
							verticesVertexPositionNormalDiffuseTexture[i] = new VertexPositionNormalDiffuseTexture(position, normal, diffuse, textureCoordinate);
						}
                        break;
                    //TODO: Hacky
                    case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE:
                        verticesVertexPositionNormalDiffuseTexture = new VertexPositionNormalDiffuseTexture[VertexCount];
                        for (int i = 0; i < VertexCount; i++)
                        {
                            var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            var normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            var diffuse = reader.ReadUInt32();
                            verticesVertexPositionNormalDiffuseTexture[i] = new VertexPositionNormalDiffuseTexture(position, normal, diffuse, Vector2.Zero);
                        }
                        break;
                    case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX2: //(D3DFVF)0x0212:
                        verticesVertexPositionNormalTextureTwo = new VertexPositionNormalTextureTwo[VertexCount];
                        for (int i = 0; i < VertexCount; i++) verticesVertexPositionNormalTextureTwo[i] = new VertexPositionNormalTextureTwo(reader);
                        break;
                    case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX2: //(D3DFVF)0x0252:
                        verticesVertexPositionNormalDiffuseTextureTwo = new VertexPositionNormalDiffuseTextureTwo[VertexCount];
                        for (int i = 0; i < VertexCount; i++) verticesVertexPositionNormalDiffuseTextureTwo[i] = new VertexPositionNormalDiffuseTextureTwo(reader);
                        break;
                    default:
                        throw new FileContentException("UTF:VMeshData", "FVF 0x" + ((int)FlexibleVertexFormat).ToString("X") + " not supported.");
                }
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
            switch (FlexibleVertexFormat)
            {
                case D3DFVF.XYZ: //(D3DFVF)0x0002:
                    SetResource(cache.AllocateVertices(verticesVertexPosition, Indices));
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL: //(D3DFVF)0x0012:
                    SetResource(cache.AllocateVertices(verticesVertexPositionNormal, Indices));
                    break;
                case D3DFVF.XYZ | D3DFVF.TEX1: //(D3DFVF)0x0102:
                    SetResource(cache.AllocateVertices(verticesVertexPositionTexture, Indices));
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX1: //(D3DFVF)0x0112:
                    SetResource(cache.AllocateVertices(verticesVertexPositionNormalTexture, Indices));
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE:
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX1: //(D3DFVF)0x0152:
                    SetResource(cache.AllocateVertices(verticesVertexPositionNormalDiffuseTexture, Indices));
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX2: //(D3DFVF)0x0212:
                    SetResource(cache.AllocateVertices(verticesVertexPositionNormalTextureTwo, Indices));
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX2: //(D3DFVF)0x0252:
                    SetResource(cache.AllocateVertices(verticesVertexPositionNormalDiffuseTextureTwo, Indices));
                    break;
            }
        }

        public override string ToString()
        {
            return FlexibleVertexFormat.ToString();
        }
    }
}
