// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Utf.Mat;
namespace LibreLancer.Utf.Vms
{
    /// <summary>
    /// Represents a VMesh (.vms) file
    /// </summary>
    public class VmsFile : UtfFile
    {
        public Dictionary<uint, VMeshData> Meshes { get; private set; }

        public VmsFile()
        {
            Meshes = new Dictionary<uint, VMeshData>();
        }

        public VmsFile(IntermediateNode vMeshLibrary)
            : this()
        {
            setMeshes(vMeshLibrary);
        }

        private void setMeshes(IntermediateNode vMeshLibrary)
        {
            foreach (IntermediateNode vmsNode in vMeshLibrary)
            {
                var vMeshDataNode =
                    vmsNode.FirstOrDefault(x => x.Name.Equals("VMeshData", StringComparison.OrdinalIgnoreCase));
                if (vMeshDataNode == null) {
                    FLLog.Error("VMS", "Invalid VMeshLibrary: No VMeshData: " + vmsNode.Name);
                    continue;
                }
                LeafNode vmsdat = vmsNode[0] as LeafNode;
                if (vmsdat == null)
                {
                    FLLog.Error("VMS", "Invalid VMeshLibrary: VMeshData has no bytes: " + vmsNode.Name);
                }
                else
                {
                    Meshes.Add(CrcTool.FLModelCrc(vmsNode.Name), new VMeshData(vmsdat.DataSegment, vmsNode.Name));
                }
            }
        }
    }
}
