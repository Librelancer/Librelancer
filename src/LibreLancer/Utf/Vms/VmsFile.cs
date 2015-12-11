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

using OpenTK;
using LibreLancer.Utf.Mat;
namespace LibreLancer.Utf.Vms
{
    /// <summary>
    /// Represents a VMesh (.vms) file
    /// </summary>
    public class VmsFile : UtfFile, ILibFile
    {
        public Dictionary<uint, VMeshData> Meshes { get; private set; }

        public VmsFile()
        {
            Meshes = new Dictionary<uint, VMeshData>();
        }

        public VmsFile(string path, ILibFile materialLibrary)
            : this()
        {
            foreach (IntermediateNode node in parseFile(path))
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "vmeshlibrary":
                        IntermediateNode vMeshLibrary = node as IntermediateNode;
                        setMeshes(vMeshLibrary, materialLibrary);
                        break;
                    default:
                        throw new Exception("Invalid node in vms root: " + node.Name);
                }
            }
        }

        public VmsFile(IntermediateNode vMeshLibrary, ILibFile materialLibrary)
            : this()
        {
            setMeshes(vMeshLibrary, materialLibrary);
        }

        private void setMeshes(IntermediateNode vMeshLibrary, ILibFile materialLibrary)
        {
            foreach (IntermediateNode vmsNode in vMeshLibrary)
            {
                if (vmsNode.Count != 1) throw new Exception("Invalid VMeshLibrary: More than one child or zero elements: " + vmsNode.Name);
                LeafNode vMeshDataNode = vmsNode[0] as LeafNode;
				Meshes.Add (CrcTool.FLModelCrc (vmsNode.Name), new VMeshData (vMeshDataNode.ByteArrayData, materialLibrary, vmsNode.Name));
            }
        }

        public TextureData FindTexture(string name)
        {
            return null;
        }

        public Material FindMaterial(uint materialId)
        {
            return null;
        }

        public VMeshData FindMesh(uint vMeshLibId)
        {
            if (Meshes.ContainsKey(vMeshLibId)) return Meshes[vMeshLibId];

            return null;
        }
    }
}