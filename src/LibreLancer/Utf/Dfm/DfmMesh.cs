// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Dfm
{
	public class DfmMesh
	{
		private Dictionary<int, DfmPart> parts;

		public List<FaceGroup> FaceGroups { get; private set; }
		public int[] PointIndices { get; private set; }
		public int[] UV0Indices { get; private set; }
		public int[] UV1Indices { get; private set; }
		public Vector3[] Points { get; private set; }
		public int[] PointBoneFirst { get; private set; }
		public int[] PointBoneCount { get; private set; }
		public int[] BoneIdChain { get; private set; }
		public float[] BoneWeightChain { get; private set; }
		public Vector3[] VertexNormals { get; private set; }
		public Vector2[] UV0 { get; private set; }
		public Vector2[] UV1 { get; private set; }

		public LeafNode UVBoneId { get; set; }
		public LeafNode UVVertexCount { get; set; }
		public LeafNode UVPlaneDistance { get; set; }
		public LeafNode BoneXToUScale { get; set; }
		public LeafNode BoneYToVScale { get; set; }
		public LeafNode MinDU { get; set; }
		public LeafNode MaxDU { get; set; }
		public LeafNode MinDV { get; set; }
		public LeafNode MaxDV { get; set; }
		public LeafNode UVVertexId { get; set; }
		public LeafNode UVDefaultList { get; set; }

		private VertexBuffer vertexBuffer;
		private ElementBuffer elementBuffer;
		private bool ready = false;

		public DfmMesh(IntermediateNode root, ILibFile materialLibrary, Dictionary<int, DfmPart> parts)
		{
			this.parts = parts;
			FaceGroups = new List<FaceGroup>();

			foreach (IntermediateNode node in root)
			{
				switch (node.Name.ToLowerInvariant())
				{
				case "face_groups":
					IntermediateNode faceGroupsNode = node as IntermediateNode;
					foreach (Node faceGroupNode in faceGroupsNode)
					{
						if (faceGroupNode.Name.ToLowerInvariant() == "count")
						{
							// ignore
						}
						else if (faceGroupNode.Name.StartsWith("group", StringComparison.OrdinalIgnoreCase))
						{
							FaceGroups.Add(new FaceGroup(faceGroupNode as IntermediateNode, materialLibrary));
						}
						else throw new Exception("Invalid node in " + faceGroupsNode.Name + ": " + faceGroupNode.Name);
					}
					break;
				case "geometry":
					foreach (LeafNode geometrySubNode in node)
					{
						switch (geometrySubNode.Name.ToLowerInvariant())
						{
						case "point_indices":
							PointIndices = geometrySubNode.Int32ArrayData;
							break;
						case "uv0_indices":
							UV0Indices = geometrySubNode.Int32ArrayData;
							break;
						case "uv1_indices":
							UV1Indices = geometrySubNode.Int32ArrayData;
							break;
						case "points":
							Points = geometrySubNode.Vector3ArrayData;
							break;
						case "point_bone_first":
							PointBoneFirst = geometrySubNode.Int32ArrayData;
							break;
						case "point_bone_count":
							PointBoneCount = geometrySubNode.Int32ArrayData;
							break;
						case "bone_id_chain":
							BoneIdChain = geometrySubNode.Int32ArrayData;
							break;
						case "bone_weight_chain":
							BoneWeightChain = geometrySubNode.SingleArrayData;
							break;
						case "vertex_normals":
							VertexNormals = geometrySubNode.Vector3ArrayData;
							break;
						case "uv0":
							UV0 = geometrySubNode.Vector2ArrayData;
							break;
						case "uv1":
							UV1 = geometrySubNode.Vector2ArrayData;
							break;
						case "uv_bone_id":
							UVBoneId = geometrySubNode;
							break;
						case "uv_vertex_count":
							UVVertexCount = geometrySubNode;
							break;
						case "uv_plane_distance":
							UVPlaneDistance = geometrySubNode;
							break;
						case "bone_x_to_u_scale":
							BoneXToUScale = geometrySubNode;
							break;
						case "bone_y_to_v_scale":
							BoneYToVScale = geometrySubNode;
							break;
						case "min_du":
							MinDU = geometrySubNode;
							break;
						case "max_du":
							MaxDU = geometrySubNode;
							break;
						case "min_dv":
							MinDV = geometrySubNode;
							break;
						case "max_dv":
							MaxDV = geometrySubNode;
							break;
						case "uv_vertex_id":
							UVVertexId = geometrySubNode;
							break;
						case "uv_default_list":
							UVDefaultList = geometrySubNode;
							break;
						default: throw new Exception("Invalid node in " + node.Name + ": " + geometrySubNode.Name);
						}
					}
					break;
				default: throw new Exception("Invalid node in " + root.Name + ": " + node.Name);
				}
			}
		}

		public void Initialize(ResourceManager cache)
		{
			List<DfmVertex> vertices = new List<DfmVertex>();
			for (int i = 0; i < PointIndices.Length; i++)
			{
				vertices.Add(new DfmVertex(Points[PointIndices[i]], VertexNormals[PointIndices[i]], UV0[UV0Indices[i]], PointBoneFirst[PointIndices[i]], PointBoneCount[PointIndices[i]]));
			}

			vertexBuffer = new VertexBuffer(typeof(DfmVertex), vertices.Count);
			vertexBuffer.SetData<DfmVertex>(vertices.ToArray());

			int indexCount = 0;
			foreach (FaceGroup faceGroup in FaceGroups)
			{
				faceGroup.StartIndex = indexCount;
				faceGroup.Initialize(cache);
				indexCount += faceGroup.TriangleStripIndices.Length;
			}
			var indices = new ushort[indexCount];
			elementBuffer = new ElementBuffer(indexCount);
			indexCount = 0;
			foreach (FaceGroup faceGroup in FaceGroups)
			{
				faceGroup.TriangleStripIndices.CopyTo(indices, indexCount);
				indexCount += faceGroup.TriangleStripIndices.Length;
			}
			elementBuffer.SetData(indices);
			vertexBuffer.SetElementBuffer(elementBuffer);
			ready = true;
		}

		public void Resized()
		{
			if (ready) foreach (FaceGroup faceGroup in FaceGroups) faceGroup.Resized();
		}

		public void Update(ICamera camera, TimeSpan delta)
		{
			if (ready) foreach (FaceGroup faceGroup in FaceGroups) faceGroup.Update (camera);
		}

		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, Lighting light, Material overrideMat = null)
		{
			foreach (FaceGroup faceGroup in FaceGroups)
			{
                faceGroup.DrawBuffer(buffer, vertexBuffer, vertexBuffer.VertexCount, world, light, overrideMat);
			}
		}

		public void Draw(RenderState rstate, Matrix4 world, Lighting lights)
		{
			if (ready)
			{
				foreach (FaceGroup faceGroup in FaceGroups)
				{
					faceGroup.Draw (rstate, vertexBuffer, vertexBuffer.VertexCount, world, lights);
				}
			}
		}
	}
}
