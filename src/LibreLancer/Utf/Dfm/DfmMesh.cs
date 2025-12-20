// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.Render;
using LibreLancer.Resources;
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

		public DfmMesh(IntermediateNode root, Dictionary<int, DfmPart> parts)
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
							FaceGroups.Add(new FaceGroup(faceGroupNode as IntermediateNode));
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

        private ResourceManager res;

        public void CalculateBoneBounds(Dictionary<int, DfmPart> parts)
        {
            foreach (var v in parts.Values)
            {
                v.Bone.Min = new Vector3(float.MaxValue);
                v.Bone.Max = new Vector3(float.MinValue);
            }
            for (int i = 0; i < PointIndices.Length; i++)
            {
                var p = Points[PointIndices[i]];
                var first = PointBoneFirst[PointIndices[i]];
                var count = PointBoneCount[PointIndices[i]];
                for (int j = first; j < first + count; j++)
                {
                    var boneId = BoneIdChain[j];
                    if (!parts.TryGetValue(boneId, out var bone))
                        continue;
                    bone.Bone.Max = Vector3.Max(p, bone.Bone.Max);
                    bone.Bone.Min = Vector3.Min(p, bone.Bone.Min);
                }
            }
            foreach (var v in parts.Values)
            {
                if (v.Bone.Max == new Vector3(float.MinValue))
                    v.Bone.Max = Vector3.Zero;
                if (v.Bone.Min == new Vector3(float.MaxValue))
                    v.Bone.Min = Vector3.Zero;
            }
        }

		public void Initialize(ResourceManager cache, RenderContext rstate)
        {
            res = cache;
			List<DfmVertex> vertices = new List<DfmVertex>();
			for (int i = 0; i < PointIndices.Length; i++)
			{
                var first = PointBoneFirst[PointIndices[i]];
                var count = PointBoneCount[PointIndices[i]];
                int id1 = 0, id2 = 0, id3 = 0, id4 = 0;
                var weights = new Vector4(1, 0, 0, 0);
                if(count > 0) {
                    id1 = BoneIdChain[first];
                    weights.X = BoneWeightChain[first];
                }
                if (count > 1) {
                    id2 = BoneIdChain[first + 1];
                    weights.Y = BoneWeightChain[first + 1];
                }
                if (count > 2) {
                    id3 = BoneIdChain[first + 2];
                    weights.Z = BoneWeightChain[first + 2];
                }
                if (count > 3) {
                    id4 = BoneIdChain[first + 3];
                    weights.W = BoneWeightChain[first + 3];
                }
                if (count > 4) throw new NotImplementedException();
                if (id1 < 0 || id2 < 0 || id3 < 0 || id4 < 0 | id1 > 255 || id2 > 255 || id3 > 255 || id4 > 255)
                {
                    throw new IndexOutOfRangeException("Bone index is out of range (<0 or >255)");
                }
                vertices.Add(new DfmVertex(Points[PointIndices[i]], VertexNormals[PointIndices[i]], UV0[UV0Indices[i]], weights, (byte)id1, (byte)id2, (byte)id3, (byte)id4));
			}

			vertexBuffer = new VertexBuffer(rstate, typeof(DfmVertex), vertices.Count);
			vertexBuffer.SetData<DfmVertex>(vertices.ToArray());

			int indexCount = 0;
			foreach (FaceGroup faceGroup in FaceGroups)
			{
				faceGroup.StartIndex = indexCount;
				indexCount += faceGroup.TriangleStripIndices.Length;
			}
			var indices = new ushort[indexCount];
			elementBuffer = new ElementBuffer(rstate, indexCount);
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

        public void DrawBuffer(CommandBuffer buffer, Matrix4x4 world, Lighting light, Material overrideMat = null)
        {
            var wh = buffer.WorldBuffer.SubmitMatrix(ref world);
			foreach (FaceGroup faceGroup in FaceGroups)
            {
                var mat = overrideMat ?? res.FindMaterial(CrcTool.FLModelCrc(faceGroup.MaterialName));
                buffer.AddCommand(
                    mat.Render,
                    null,
                    wh,
                    light,
                    vertexBuffer,
                    PrimitiveTypes.TriangleStrip,
                    0,
                    faceGroup.StartIndex,
                    faceGroup.TriangleStripIndices.Length - 2,
                    SortLayers.OPAQUE,
                    0,
                    skinning
                );
			}
		}

        private DfmSkinning skinning;
        public void SetSkinning(DfmSkinning skinning)
        {
            this.skinning = skinning;
        }
    }
}
