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