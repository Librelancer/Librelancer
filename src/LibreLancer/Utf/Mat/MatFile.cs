// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Utf.Vms;

namespace LibreLancer.Utf.Mat
{
	public class MatFile : UtfFile, ILibFile
	{
		private ILibFile additionalTextureLibrary;

		public Dictionary<uint, Material> Materials { get; private set; }

		public TxmFile TextureLibrary { get; private set; }

		public MatFile (ILibFile additionalTextureLibrary)
		{
			this.additionalTextureLibrary = additionalTextureLibrary;

			Materials = new Dictionary<uint, Material> ();
		}

		public MatFile (IntermediateNode materialLibraryNode, ILibFile additionalTextureLibrary)
			: this (additionalTextureLibrary)
		{
			setMaterials (materialLibraryNode);
		}

		private void setMaterials (IntermediateNode materialLibraryNode)
		{
			//TODO: int count = 0;
			foreach (Node materialNode in materialLibraryNode) {
				if (materialNode is IntermediateNode) {
					uint materialId = CrcTool.FLModelCrc (materialNode.Name);
					if (!Materials.ContainsKey (materialId))
						Materials.Add (materialId, Material.FromNode (materialNode as IntermediateNode, this));
				}
				//else if (subNode.Name.Equals("material count", StringComparison.OrdinalIgnoreCase))
				//count = (subNode as LeafNode).getIntegerBlaBLubb;
			}
			//if (count != materials.Count)
			//throw new Exception("Invalid material count: " + count + " != " + materials.Count);
		}

		public Texture FindTexture (string name)
		{
			return additionalTextureLibrary.FindTexture (name);
		}

		public Material FindMaterial (uint materialId)
		{
			if (Materials.ContainsKey (materialId))
				return Materials [materialId];

			return null;
		}

		public VMeshData FindMesh (uint vMeshLibId)
		{
			return null;
		}
	}
}