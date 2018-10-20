// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Utf.Dfm
{
	public class FaceGroup
	{

		private ILibFile materialLibrary;

		private string materialName;
		private Material material;
		public Material Material
		{
			get
			{
				if (material == null)
				{
					material = materialLibrary.FindMaterial(CrcTool.FLModelCrc(materialName));
				}

				return material;
			}
		}

		public int StartIndex;
		public ushort[] TriangleStripIndices { get; private set; }
		public ushort[] EdgeIndices { get; private set; }
		public float[] EdgeAngles { get; private set; }

		private bool ready = false;

		public FaceGroup(IntermediateNode root, ILibFile materialLibrary)
		{
			this.materialLibrary = materialLibrary;

			foreach (LeafNode node in root)
			{
				switch (node.Name.ToLowerInvariant())
				{
				case "material_name": materialName = node.StringData;
					break;
				case "tristrip_indices": TriangleStripIndices = node.UInt16ArrayData;
					break;
				case "edge_indices": EdgeIndices = node.UInt16ArrayData;
					break;
				case "edge_angles": EdgeAngles = node.SingleArrayData;
					break;
				default: throw new Exception("Invalid node in " + root.Name + ": " + node.Name);
				}
			}
		}

		public void Initialize(ResourceManager cache)
		{
			ready = true;
		}

		public void Resized()
		{
			if (ready) Material.Resized();
		}

		public void Update(ICamera camera)
		{
			if (ready) Material.Update(camera);
		}

		public void DrawBuffer(CommandBuffer buffer, VertexBuffer vbo, int vertexCount, Matrix4 world, Lighting lights, Material overrideMat)
		{
			buffer.AddCommand(
				(overrideMat ?? Material).Render,
				null,
				world,
				lights,
				vbo,
				PrimitiveTypes.TriangleStrip,
				0,
				StartIndex,
				TriangleStripIndices.Length - 2,
				SortLayers.OPAQUE,
				0
			);
		}

		public void Draw(RenderState rstate, VertexBuffer vbo, int vertexCount, Matrix4 world, Lighting lights)
		{
			if (ready)
			{
				//vbo.SetElementBuffer(triangleStripIndexBuffer);
				//Material.Render.World = world;
				//Material.Render.Use (rstate, vbo.VertexType, lights);
				//vbo.Draw (PrimitiveTypes.TriangleStrip, 0, vertexCount, TriangleStripIndices.Length - 2);
				//Material.Draw(D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX1, PrimitiveTypes.TriangleStrip, 0, vertexCount, 0, TriangleStripIndices.Length - 2, ambient, lights, world);
			}
		}
	}
}
