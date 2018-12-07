// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Vms;
namespace LibreLancer.Utf.Cmp
{
    public class VMeshWire
    {
        public uint MeshCRC;
        public ushort VertexOffset;
        public ushort NumVertices;
        public ushort NumIndices;
        public ushort MaxVertex;
        public ushort[] Indices;

        public Vector3[] Lines;
        
        public VMeshWire(IntermediateNode node, ILibFile parent)
        {
            if(node.Count != 1 || 
            !(node[0] is LeafNode) || 
                !string.Equals(node[0].Name,"vwiredata",StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Invalid VMeshWire node");
            }
            var wiredata = ((LeafNode)node[0]).ByteArrayData;
            ReadWireData(wiredata);
        }

        public void Initialize(ILibFile res)
        {
            if (Lines != null) return;
            VMeshData vms;
            if((vms = res.FindMesh(MeshCRC)) == null) {
                Lines = new Vector3[0];
                FLLog.Error("Vms", "VMeshWire cannot find VMeshData CRC 0x" + MeshCRC.ToString("X"));
                return;
            }
            Lines = new Vector3[NumIndices];
            for(int i = 0; i < NumIndices; i++)
            {
                var idx = Indices[i] + VertexOffset;
                //TODO: This is ridiculous
                if (vms.verticesVertexPosition != null) Lines[i] = vms.verticesVertexPosition[idx].Position;
                if (vms.verticesVertexPositionNormal != null) Lines[i] = vms.verticesVertexPositionNormal[idx].Position;
                if (vms.verticesVertexPositionTexture != null) Lines[i] = vms.verticesVertexPositionTexture[idx].Position;
                if (vms.verticesVertexPositionNormalTexture != null) Lines[i] = vms.verticesVertexPositionNormalTexture[idx].Position;
                if (vms.verticesVertexPositionNormalTextureTwo != null) Lines[i] = vms.verticesVertexPositionNormalTextureTwo[idx].Position;
                if (vms.verticesVertexPositionNormalDiffuseTexture != null) Lines[i] = vms.verticesVertexPositionNormalDiffuseTexture[idx].Position;
                if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null) Lines[i] = vms.verticesVertexPositionNormalDiffuseTextureTwo[idx].Position;
            }
        }

        const int HEADER_SIZE = 16;
        unsafe void ReadWireData(byte[] array)
        {
            if (array.Length < HEADER_SIZE)
                throw new Exception("Invalid VWireData Node (size<HEADER_SIZE)");
            fixed(byte* b = array)
            {
                var pInt = (uint*)b;
                var pShort = (ushort*)b;
                //
                if (pInt[0] != HEADER_SIZE) throw new Exception("Invalid VWireData Node (header size != 16)");
                MeshCRC = pInt[1];
                VertexOffset = pShort[4];
                NumVertices = pShort[5];
                NumIndices = pShort[6];
                MaxVertex = pShort[7];
                if (array.Length - HEADER_SIZE < (NumIndices * 2)) throw new Exception("Invalid VWireData Node (insufficient data for NumIndices)");
                Indices = new ushort[NumIndices];
                for(int i = 0; i < NumIndices; i++)
                {
                    Indices[i] = pShort[i + 8];
                }
            }
        }
    }
}
