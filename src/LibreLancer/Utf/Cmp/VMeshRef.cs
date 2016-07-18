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
 * Data structure by Mario Brito from FLModelTool by Anton (Xtreme Team Studios)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;

//using FLCommon;

//using FLParser;
//using FLParser.Utf;

using LibreLancer.Utf.Vms;
//using FLApi.Universe;

namespace LibreLancer.Utf.Cmp
{
    public class VMeshRef : IDrawable
    {
        private ILibFile vMeshLibrary;
        private bool ready = false;

        public uint HeaderSize { get; private set; }

        private uint vMeshLibId;
        private VMeshData mesh;
        public VMeshData Mesh
        {
            get
            {
                if (mesh == null) mesh = vMeshLibrary.FindMesh(vMeshLibId);
                return mesh;
            }
        }

        public ushort StartVertex { get; private set; }
        public ushort VertexCount { get; private set; }
        public ushort StartIndex { get; private set; }
        public ushort IndexCount { get; private set; }
        public ushort StartMesh { get; private set; }
        public ushort MeshCount { get; private set; }

        public BoundingBox BoundingBox { get; private set; }
        public Vector3 Center { get; private set; }
        public float Radius { get; private set; }

        private int endMesh;

        public VMeshRef(byte[] data, ILibFile vMeshLibrary)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (vMeshLibrary == null) throw new ArgumentNullException("vMeshLibrary");

            this.vMeshLibrary = vMeshLibrary;

            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                mesh = null;

                HeaderSize = reader.ReadUInt32();
                vMeshLibId = reader.ReadUInt32();
                StartVertex = reader.ReadUInt16();
                VertexCount = reader.ReadUInt16();
                StartIndex = reader.ReadUInt16();
                IndexCount = reader.ReadUInt16();
                StartMesh = reader.ReadUInt16();
                MeshCount = reader.ReadUInt16();

                Vector3 max = Vector3.Zero;
                Vector3 min = Vector3.Zero;

                max.X = reader.ReadSingle();
                min.X = reader.ReadSingle();
                max.Y = reader.ReadSingle();
                min.Y = reader.ReadSingle();
                max.Z = reader.ReadSingle();
                min.Z = reader.ReadSingle();

                BoundingBox = new BoundingBox(min, max);

                Center = ConvertData.ToVector3(reader);
                Radius = reader.ReadSingle();

                endMesh = StartMesh + MeshCount;
            }
        }

		public void Initialize(ResourceManager cache)
        {
			Mesh.Initialize (StartMesh, endMesh, cache);
            ready = true;
        }

        public void Resized()
        {
            if (ready) Mesh.DeviceReset(StartMesh, endMesh);
        }

		public void Update(ICamera camera, TimeSpan delta)
        {
            if (ready) Mesh.Update(camera, StartMesh, endMesh);
        }

		public float GetRadius()
		{
			return Radius;
		}

		public void Draw(RenderState rstate, Matrix4 world, Lighting light)
        {
			if (Mesh.FlexibleVertexFormat == (D3DFVF.XYZ | D3DFVF.NORMAL))
				return;
            if (ready) Mesh.Draw(rstate, StartMesh, endMesh, StartVertex, world, light);
        }

		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, Lighting light)
		{
			if (Mesh.FlexibleVertexFormat == (D3DFVF.XYZ | D3DFVF.NORMAL))
				return;
			if (ready) Mesh.DrawBuffer(buffer, StartMesh, endMesh, StartVertex, world, light, Center);
		}

        public override string ToString()
        {
            return "VMeshRef";
        }
    }
}