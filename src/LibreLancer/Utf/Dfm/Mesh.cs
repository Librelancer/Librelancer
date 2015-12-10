/* The contents of this file a
 * re subject to the Mozilla Public License
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
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using LibreLancer.Vertices;

namespace LibreLancer.Utf.Dfm
{
	public class Mesh : IDrawable
	{
		private Dictionary<int, Part> parts;

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
		private bool ready = false;

		public Mesh(IntermediateNode root, ILibFile materialLibrary, Dictionary<int, Part> parts)
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

		public void Initialize()
		{
			List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
			for (int i = 0; i < PointIndices.Length; i++)
			{
				vertices.Add(new VertexPositionNormalTexture(Points[PointIndices[i]], VertexNormals[PointIndices[i]], UV0[UV0Indices[i]]));
			}

			vertexBuffer = new VertexBuffer(typeof(VertexPositionNormalTexture), vertices.Count);
			vertexBuffer.SetData<VertexPositionNormalTexture>(vertices.ToArray());

			foreach (FaceGroup faceGroup in FaceGroups)
				faceGroup.Initialize ();

			ready = true;
		}

		public void Resized()
		{
			if (ready) foreach (FaceGroup faceGroup in FaceGroups) faceGroup.Resized();
		}

		public void Update(Camera camera)
		{
			if (ready) foreach (FaceGroup faceGroup in FaceGroups) faceGroup.Update(camera);
		}

		public void Draw(Matrix4 world, Lighting lights)
		{
			if (ready)
			{
				foreach (FaceGroup faceGroup in FaceGroups)
				{
					faceGroup.Draw (vertexBuffer, vertexBuffer.VertexCount, world, lights);
				}
			}
		}
	}
}
