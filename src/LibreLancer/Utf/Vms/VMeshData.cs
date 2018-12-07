// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;

using LibreLancer.Vertices;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
namespace LibreLancer.Utf.Vms
{
    public class VMeshData
    {
        private bool ready;

        // Data header - 16 bytes long
        public uint MeshType { get; private set; } //0x00000001
        public uint SurfaceType { get; private set; } //0x00000004
        public ushort MeshCount { get; private set; }
        public ushort IndexCount { get; private set; }
        public D3DFVF FlexibleVertexFormat { get; private set; } //FVF used for rendering
        public D3DFVF OriginalFVF { get; private set; } //FVF stored in the file
        public ushort VertexCount { get; private set; }

        /// <summary>
        /// A list of meshes in the mesh data
        /// </summary>
        public List<TMeshHeader> Meshes { get; private set; }

		public ushort[] Indices;
        /// <summary>
        /// EDITOR USE ONLY - Made null after init
        /// </summary>
        public uint[] Diffuse;
        /// <summary>
        /// A list of triangles in the mesh data
        /// </summary>
        public ElementBuffer ElementBuffer { get; private set; }

        public VertexPosition[] verticesVertexPosition { get; private set; }
        public VertexPositionNormal[] verticesVertexPositionNormal { get; private set; }
        public VertexPositionTexture[] verticesVertexPositionTexture { get; private set; }
        public VertexPositionNormalTexture[] verticesVertexPositionNormalTexture { get; private set; }
        public VertexPositionNormalDiffuseTexture[] verticesVertexPositionNormalDiffuseTexture { get; private set; }
        public VertexPositionNormalTextureTwo[] verticesVertexPositionNormalTextureTwo { get; private set; }
        public VertexPositionNormalDiffuseTextureTwo[] verticesVertexPositionNormalDiffuseTextureTwo { get; private set; }

        VertexBuffer _vertexBuffer;
        /// <summary>
        /// A list of Vertices in the mesh data
        /// </summary>
        public VertexBuffer VertexBuffer { get { return _vertexBuffer; } }
		string vmsname;

		public VMeshData(byte[] data, ILibFile materialLibrary, string name)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (materialLibrary == null) throw new ArgumentNullException("materialLibrary");
			vmsname = name;
            ready = false;

            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {

                // Read the data header.
                MeshType = reader.ReadUInt32();
                SurfaceType = reader.ReadUInt32();
                MeshCount = reader.ReadUInt16();
                IndexCount = reader.ReadUInt16();
                FlexibleVertexFormat = (D3DFVF)reader.ReadUInt16();
                OriginalFVF = FlexibleVertexFormat;
                VertexCount = reader.ReadUInt16();

                // Read the mesh headers.
                Meshes = new List<TMeshHeader>();
                int triangleStartOffset = 0;
                for (int count = 0; count < MeshCount; count++)
                {
                    TMeshHeader item = new TMeshHeader(reader, triangleStartOffset, materialLibrary);
                    triangleStartOffset += item.NumRefVertices;
                    Meshes.Add(item);
                }

                // Read the triangle data
                Indices = new ushort[IndexCount];
                for (int i = 0; i < IndexCount; i++) Indices[i] = reader.ReadUInt16();

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
                        verticesVertexPositionNormalTexture = new VertexPositionNormalTexture[VertexCount];
                        for (int i = 0; i < VertexCount; i++)
                        {
                            Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            Vector3 normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
							Vector2 textureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
                            verticesVertexPositionNormalTexture[i] = new VertexPositionNormalTexture(position, normal, textureCoordinate);
                        }
                        break;
                    case D3DFVF.XYZ | D3DFVF.DIFFUSE | D3DFVF.TEX1: //(D3DFVF)0x0142:
                        verticesVertexPositionNormalDiffuseTexture = new VertexPositionNormalDiffuseTexture[VertexCount];
                        Diffuse = new uint[VertexCount];
                        for (int i = 0; i < VertexCount; i++)
                        {
                            Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            Diffuse[i] = reader.ReadUInt32();
							Vector2 textureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
                            verticesVertexPositionNormalDiffuseTexture[i] = new VertexPositionNormalDiffuseTexture(position, Vector3.One, Diffuse[i], textureCoordinate);
                        }
						FlexibleVertexFormat |= D3DFVF.NORMAL;
                        break;
                    case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX1: //(D3DFVF)0x0152:
                        verticesVertexPositionNormalDiffuseTexture = new VertexPositionNormalDiffuseTexture[VertexCount];
                        Diffuse = new uint[VertexCount];
						for (int i = 0; i < VertexCount; i++)
						{
							var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
							var normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            Diffuse[i] = reader.ReadUInt32();
							Vector2 textureCoordinate = new Vector2(reader.ReadSingle(), 1 - reader.ReadSingle());
							verticesVertexPositionNormalDiffuseTexture[i] = new VertexPositionNormalDiffuseTexture(position, normal, Diffuse[i], textureCoordinate);
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
                    /*case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX4: //(D3DFVF)0x0412:
                        for (int i = 0; i < VertexCount; i++) vertices[i] = new VertexPositionNormalTextureTangentBinormal(reader);
                        break;*/
                    default:
                        throw new FileContentException("UTF:VMeshData", "FVF 0x" + FlexibleVertexFormat + " not supported.");
                }
            }
        }

		public void Initialize(ushort startMesh, int endMesh, ResourceManager cache)
		{
			for (ushort i = startMesh; i < endMesh; i++)
			{
				Meshes[i].Initialize(cache);
			}
			if (VertexBuffer != null)
			{
				//Don't upload vmeshdata a million times to ram.
				return;
			}
            Diffuse = null;
            GenerateVertexBuffer(cache);
			ready = true;
		}
        public int IndexOffset = 0;
        public int VertexOffset = 0;
        public IndexResourceHandle IndexHandle;
        void GenerateVertexBuffer(ResourceManager cache)
        {
          
            switch (FlexibleVertexFormat)
            {
                case D3DFVF.XYZ: //(D3DFVF)0x0002:
                    cache.AllocateVertices(verticesVertexPosition, Indices, out IndexOffset, out VertexOffset, out _vertexBuffer, out IndexHandle);
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL: //(D3DFVF)0x0012:
                    cache.AllocateVertices(verticesVertexPositionNormal, Indices, out IndexOffset, out VertexOffset, out _vertexBuffer, out IndexHandle);
                    break;
                case D3DFVF.XYZ | D3DFVF.TEX1: //(D3DFVF)0x0102:
                    cache.AllocateVertices(verticesVertexPositionTexture, Indices, out IndexOffset, out VertexOffset, out _vertexBuffer, out IndexHandle);
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX1: //(D3DFVF)0x0112:
                    cache.AllocateVertices(verticesVertexPositionNormalTexture, Indices, out IndexOffset, out VertexOffset, out _vertexBuffer, out IndexHandle);
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX1: //(D3DFVF)0x0152:
                    cache.AllocateVertices(verticesVertexPositionNormalDiffuseTexture, Indices, out IndexOffset, out VertexOffset, out _vertexBuffer, out IndexHandle);
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX2: //(D3DFVF)0x0212:
                    cache.AllocateVertices(verticesVertexPositionNormalTextureTwo, Indices, out IndexOffset, out VertexOffset, out _vertexBuffer, out IndexHandle);
                    break;
                case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX2: //(D3DFVF)0x0252:
                    cache.AllocateVertices(verticesVertexPositionNormalDiffuseTextureTwo, Indices, out IndexOffset, out VertexOffset, out _vertexBuffer, out IndexHandle);
                    break;
                /*case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX4: //(D3DFVF)0x0412:
                    VertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTextureTangentBinormal), VertexCount, BufferUsage.WriteOnly);
                    VertexBuffer.SetData<VertexPositionNormalTextureTangentBinormal>(verticesVertexPositionNormalTextureTangentBinormal);
                    break;*/
            }

        }

        public void DeviceReset(ushort startMesh, int endMesh)
        {
            if (ready)
            {
                for (ushort i = startMesh; i < endMesh; i++)
                {
                    Meshes[i].DeviceReset();
                }
            }
        }
		ICamera camera;
        public void Update(ICamera camera, ushort startMesh, int endMesh)
        {
            if (ready)
            {
				this.camera = camera;
                for (ushort i = startMesh; i < endMesh; i++)
                {
                    Meshes[i].Update(camera);
                }
            }
        }

		public void Draw(RenderState rstate, ushort startMesh, int endMesh, ushort startVertex, Matrix4 world, Lighting light, MaterialAnimCollection mc)
        {
            if (ready)
			{
                for (ushort i = startMesh; i < endMesh; i++)
                {
					Meshes [i].Draw (rstate, VertexBuffer, startVertex + VertexOffset, IndexOffset, world, light, mc);
                }
            }
        }
		public void DrawBuffer(CommandBuffer buff, ushort startMesh, int endMesh, ushort startVertex, Matrix4 world, ref Lighting light, Vector3 center, MaterialAnimCollection mc, Material overrideMat = null)
		{
			if (ready)
			{
				for (ushort i = startMesh; i < endMesh; i++)
				{
					Meshes[i].DrawBuffer(buff, this, VertexOffset, startVertex, IndexOffset, world, ref light, mc, overrideMat);
				}
			}
		}

		public void DepthPrepass(RenderState rstate, ushort startMesh, int endMesh, ushort startVertex, Matrix4 world, MaterialAnimCollection mc)
		{
			if (ready)
			{
				for (ushort i = startMesh; i < endMesh; i++)
				{
					Meshes[i].DepthPrepass(rstate, this, startVertex + VertexOffset, IndexOffset, world, mc);
				}
			}
		}

        public override string ToString()
        {
            return FlexibleVertexFormat.ToString();
        }
    }
}