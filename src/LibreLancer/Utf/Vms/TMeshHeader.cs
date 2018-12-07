// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;
namespace LibreLancer.Utf.Vms
{
    /// <summary>
    /// Repeated no_meshes times in segment - 12 bytes
    /// </summary>
    public class TMeshHeader
    {
        private ILibFile materialLibrary;
        //private static NullMaterial nullMaterial;

        /// <summary>
        /// CRC of texture name for mesh
        /// </summary>
        private uint MaterialId;
        private Material material;
		Material defaultMaterial;
        public Material Material
        {
            get
            {
				if (material != null && !material.Loaded) material = null;
                if (material == null) material = materialLibrary.FindMaterial(MaterialId);
                return material;
            }
        }

		public uint MaterialCrc
		{
			get
			{
				return MaterialId;
			}
		}

        public ushort StartVertex { get; private set; }
        public ushort EndVertex { get; private set; }
        public ushort NumRefVertices { get; private set; }
        public ushort Padding { get; private set; } //0x00CC

        public int TriangleStart { get; private set; }

        private int numVertices;
        private int primitiveCount;

        public TMeshHeader(BinaryReader reader, int triangleStartOffset, ILibFile materialLibrary)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (materialLibrary == null) throw new ArgumentNullException("materialLibrary");

            this.materialLibrary = materialLibrary;

            MaterialId = reader.ReadUInt32();
            StartVertex = reader.ReadUInt16();
            EndVertex = reader.ReadUInt16();
            NumRefVertices = reader.ReadUInt16();
            Padding = reader.ReadUInt16();

            TriangleStart = triangleStartOffset;

            numVertices = EndVertex - StartVertex + 1;
            primitiveCount = NumRefVertices / 3;
        }

		bool inited = false;
		public void Initialize(ResourceManager cache)
        {
			inited = true;
			defaultMaterial = cache.DefaultMaterial;
        }

        public void DeviceReset()
        {
            /*if (Material == null) nullMaterial.Resized();
			else */Material.Resized();
        }

        public void Update(ICamera camera)
        {
			/*if (Material == null) nullMaterial.Update(camera);
            else */
			if (Material != null)
				Material.Render.Camera = camera;
			else
				defaultMaterial.Render.Camera = camera;
        }
		MaterialAnimCollection lastmc;
		MaterialAnim ma;

		public void Draw(RenderState rstate, VertexBuffer buff, int startVertex, int startIndex, Matrix4 world, Lighting light, MaterialAnimCollection mc)
        {
            if (MaterialCrc == 0) return;

			if (lastmc != mc)
			{
				if (mc != null)
				{
					mc.Anims.TryGetValue(Material.Name, out ma);
					lastmc = mc;
				}
				else
					ma = null;
			}
			Material.Render.MaterialAnim = ma;
			Material.Render.World = world;
			Material.Render.Use (rstate, buff.VertexType, ref light);
			buff.Draw (PrimitiveTypes.TriangleList, startVertex + StartVertex, startIndex + TriangleStart, primitiveCount);
        }

		struct Average
		{
			public ushort StartVertex;
			public VMeshData VMesh;
			public Vector3 Point;
		}
		List<Average> averages = new List<Average>();
		Vector3 CalculateAvg(VMeshData vm, ushort sv)
		{
			for (int i = 0; i < averages.Count; i++)
				if (averages[i].StartVertex == sv && averages[i].VMesh == vm)
					return averages[i].Point;
			var v = sv + StartVertex;
			double x = 0;
			double y = 0;
			double z = 0;
			var vertType = vm.VertexBuffer.VertexType.GetType();
			for (int i = TriangleStart; i < TriangleStart + NumRefVertices; i++)
			{
				Vector3 vert = Vector3.Zero;
				int idx = vm.Indices[i] + v;
                if (vertType == typeof(VertexPosition))
                    vert = vm.verticesVertexPosition[idx].Position;
                else if (vertType == typeof(VertexPositionNormal))
                    vert = vm.verticesVertexPositionNormal[idx].Position;
                else if (vertType == typeof(VertexPositionTexture))
                    vert = vm.verticesVertexPositionTexture[idx].Position;
				else if (vertType == typeof(VertexPositionNormalDiffuseTexture))
					vert = vm.verticesVertexPositionNormalDiffuseTexture[idx].Position;
				else if (vertType == typeof(VertexPositionNormalTexture))
					vert = vm.verticesVertexPositionNormalTexture[idx].Position;
				else if (vertType == typeof(VertexPositionNormalTextureTwo))
					vert = vm.verticesVertexPositionNormalTextureTwo[idx].Position;
				else if (vertType == typeof(VertexPositionNormalDiffuseTextureTwo))
					vert = vm.verticesVertexPositionNormalDiffuseTextureTwo[idx].Position;
				else
					throw new Exception();
				x += vert.X;
				y += vert.Y;
				z += vert.Z;
			}
			x /= NumRefVertices;
			y /= NumRefVertices;
			z /= NumRefVertices;
			var avg = new Vector3((float)x, (float)y, (float)z);
			averages.Add(new Average() { VMesh = vm, StartVertex = sv, Point = avg });
			return avg;
		}

		public void DrawBuffer(CommandBuffer buffer, VMeshData data, int vertexOffset, ushort startVertex, int startIndex, Matrix4 world, ref Lighting light, MaterialAnimCollection mc, Material overrideMat = null)
		{
            if (MaterialCrc == 0) return;

			var mat = Material;
			if (mat == null)
				mat = defaultMaterial;
			if (overrideMat != null)
				mat = overrideMat;
			if (lastmc != mc)
			{
				if (mc != null)
				{
					mc.Anims.TryGetValue(mat.Name, out ma);
					lastmc = mc;
				}
				else
					ma = null;
			}
			float z = 0;
			if (mat.Render.IsTransparent)
				z = RenderHelpers.GetZ(world, mat.Render.Camera.Position, CalculateAvg(data, startVertex));
			
			buffer.AddCommand(
				mat.Render,
				ma,
				world,
				light,
				data.VertexBuffer,
				PrimitiveTypes.TriangleList,
				startVertex + vertexOffset + StartVertex,
				startIndex + TriangleStart,
				primitiveCount,
				SortLayers.OBJECT,
				z
			);
		}

		public void DepthPrepass(RenderState rstate, VMeshData data, int startVertex, int startIndex, Matrix4 world, MaterialAnimCollection mc)
		{
            if (MaterialCrc == 0) return;

			var m = Material;
            if (m == null) m = materialLibrary.FindMaterial(0);
			if (m.Render.IsTransparent)
				return;
            if (m.Render.DoubleSided)
                return; //TODO: Fix depth prepass for double-sided
			if (lastmc != mc)
			{
				if (mc != null)
				{
					mc.Anims.TryGetValue(m.Name, out ma);
					lastmc = mc;
				}
				else
					ma = null;
			}
			m.Render.MaterialAnim = ma;
			m.Render.World = world;
			m.Render.ApplyDepthPrepass(rstate);
			data.VertexBuffer.Draw(PrimitiveTypes.TriangleList, startVertex + StartVertex, startIndex + TriangleStart, primitiveCount);
		}

        public override string ToString()
        {
            string transparent = "";
            if (Material != null && Material.Render != null)
                transparent = Material.Render.IsTransparent ? "-TR" : "";
            return string.Format("[Mat:{0}{4}, Off:{1}, Start:{2}, Count:{3}]", MaterialCrc.ToString("X"), StartVertex, TriangleStart, NumRefVertices,transparent);
        }
    }
}
