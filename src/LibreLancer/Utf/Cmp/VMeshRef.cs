// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Cmp
{
    public class VMeshRef
    {
        public uint HeaderSize { get; private set; }

        private uint vMeshLibId;
		public uint MeshCrc
		{
			get
			{
				return vMeshLibId;
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

        public VMeshRef(ArraySegment<byte> data)
        {
            if (data == null) throw new ArgumentNullException("data");

            using (BinaryReader reader = new BinaryReader(data.GetReadStream()))
            {

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

        public MeshLevel CreateLevel(ResourceManager resources)
        {
            if (MeshCrc == 0) return null;
            var res = resources.FindMesh(vMeshLibId);
            if (res == null) return null;
            var (opt, dcs) = res.Optimize(StartMesh, (ushort)endMesh, StartVertex, resources);
            if (dcs == null)
            {
                dcs = new MeshDrawcall[MeshCount];
                for (int i = 0; i < MeshCount; i++)
                {
                    var t = res.Meshes[i + StartMesh];
                    dcs[i] = new MeshDrawcall()
                    {
                        MaterialCrc = t.MaterialCrc,
                        BaseVertex = StartVertex + t.StartVertex,
                        StartIndex = t.TriangleStart,
                        PrimitiveCount = t.NumRefVertices / 3,
                    };
                }
            }
            return new MeshLevel() {Drawcalls = dcs, Optimize = opt, Resource = res};
        }

        public override string ToString()
        {
            return "VMeshRef";
        }
    }
}
