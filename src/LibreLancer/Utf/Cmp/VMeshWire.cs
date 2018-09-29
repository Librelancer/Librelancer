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
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
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

        public void Initialize(ResourceManager res)
        {
            if (Lines != null) return;
            var vms = res.FindMesh(MeshCRC);
            if(vms == null)
                throw new Exception("VMeshWire CRC not referenced");
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
                if (vms.verticesVertexPositionNormalColorTexture != null) Lines[i] = vms.verticesVertexPositionNormalColorTexture[idx].Position;
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
