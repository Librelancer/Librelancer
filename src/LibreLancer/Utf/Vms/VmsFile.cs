// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;

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

        public Texture FindTexture(string name)
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