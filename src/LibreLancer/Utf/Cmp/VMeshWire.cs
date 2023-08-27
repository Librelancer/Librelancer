// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
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
        
        public VMeshWire(IntermediateNode node)
        {
            if(node.Count != 1 || 
            !(node[0] is LeafNode) || 
                !string.Equals(node[0].Name,"vwiredata",StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Invalid VMeshWire node");
            }

            ReadWireData(((LeafNode) node[0]).DataSegment);
        }
        

        public const int HEADER_SIZE = 16;
        unsafe void ReadWireData(ArraySegment<byte> data)
        {
            if (data.Count < HEADER_SIZE)
                throw new Exception("Invalid VWireData Node (size<HEADER_SIZE)");
            fixed(byte* b = data.Array)
            {
                var pInt = (uint*)(&b[data.Offset]);
                var pShort = (ushort*)(&b[data.Offset]);
                //
                if (pInt[0] != HEADER_SIZE) throw new Exception("Invalid VWireData Node (header size != 16)");
                MeshCRC = pInt[1];
                VertexOffset = pShort[4];
                NumVertices = pShort[5];
                NumIndices = pShort[6];
                MaxVertex = pShort[7];
                if (data.Count - HEADER_SIZE < (NumIndices * 2)) throw new Exception("Invalid VWireData Node (insufficient data for NumIndices)");
                Indices = new ushort[NumIndices];
                for(int i = 0; i < NumIndices; i++)
                {
                    Indices[i] = pShort[i + 8];
                }
            }
        }
    }
}
