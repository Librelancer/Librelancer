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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

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
        public Material Material
        {
            get
            {
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

		public void Initialize(ResourceManager cache)
        {
            /*if (nullMaterial == null)
            {
                nullMaterial = new NullMaterial();
                nullMaterial.Initialize();
            }*/

            //if (Material != null) Material.Initialize(cache);
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
			if(Material != null)
				Material.Render.Camera = camera;
        }
		MaterialAnimCollection lastmc;
		MaterialAnim ma;

		public void Draw(RenderState rstate, VertexBuffer buff, ushort startVertex, Matrix4 world, Lighting light, MaterialAnimCollection mc)
        {
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
			Material.Render.Use (rstate, buff.VertexType, light);
			buff.Draw (PrimitiveTypes.TriangleList, startVertex + StartVertex, TriangleStart, primitiveCount);
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
				if (vertType == typeof(VertexPositionNormalColorTexture))
					vert = vm.verticesVertexPositionNormalColorTexture[idx].Position;
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

		public void DrawBuffer(CommandBuffer buffer, VMeshData data, ushort startVertex, Matrix4 world, Lighting light, MaterialAnimCollection mc)
		{
			if (Material == null)
				return;
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
			float z = 0;
			if (Material.Render.IsTransparent)
				z = RenderHelpers.GetZ(world, Material.Render.Camera.Position, CalculateAvg(data, startVertex));
			
			buffer.AddCommand(
				Material.Render,
				ma,
				world,
				light,
				data.VertexBuffer,
				PrimitiveTypes.TriangleList,
				startVertex + StartVertex,
				TriangleStart,
				primitiveCount,
				SortLayers.OBJECT,
				z
			);
		}

		public void DepthPrepass(RenderState rstate, VMeshData data, ushort startVertex, Matrix4 world, MaterialAnimCollection mc)
		{
			if (Material == null)
				return;
			if (Material.Render.IsTransparent)
				return;
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
			Material.Render.ApplyDepthPrepass(rstate);
			data.VertexBuffer.Draw(PrimitiveTypes.TriangleList, startVertex + StartVertex, TriangleStart, primitiveCount);
		}
    }
}
