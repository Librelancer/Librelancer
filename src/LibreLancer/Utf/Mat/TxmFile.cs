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
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;

using LibreLancer.Utf.Vms;

namespace LibreLancer.Utf.Mat
{
	public class TxmFile : UtfFile, ILibFile
	{
		public Dictionary<string, TextureData> Textures { get; private set; }
        public TxmFile()
        {
            Textures = new Dictionary<string, TextureData>();
        }

        public TxmFile(string path)
            : this()
        {
            foreach (Node node in parseFile(path))
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "texture library":
                        IntermediateNode textureLibraryNode = node as IntermediateNode;
                        setTextures(textureLibraryNode);
                        break;
                    case "exporter version":
                        break;
                    default:
                        throw new Exception("Invalid node in txm root: " + node.Name);
                }
            }
        }

        public TxmFile(IntermediateNode textureLibraryNode)
            : this()
        {
            setTextures(textureLibraryNode);
        }

        private void setTextures(IntermediateNode textureLibraryNode)
        {
            foreach (IntermediateNode textureNode in textureLibraryNode)
            {
                LeafNode child = textureNode[textureNode.Count - 1] as LeafNode;
                if (child == null) throw new Exception("Invalid texture library");

				TextureData data = new TextureData (child, textureNode.Name);
                if (data == null) throw new Exception("Invalid texture library");

				string key = textureNode.Name;
				Textures.Add(key, data);
            }
        }

        public Texture FindTexture(string name)
        {
			return null;
        }

        public Material FindMaterial(uint materialId)
        {
            return null;
        }

        public Material FindMaterial(string name)
        {
            return null;
        }

        public VMeshData FindMesh(uint vMeshLibId)
        {
            return null;
        }
    }
}