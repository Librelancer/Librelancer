// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Cmp
{
    public class VMeshRef
    {
        private ILibFile vMeshLibrary;
        private bool ready = false;

        public uint HeaderSize { get; private set; }

        private uint vMeshLibId;
		public uint MeshCrc
		{
			get
			{
				return vMeshLibId;
			}
		}
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

        class MaterialCounter
        {
            public uint Material;
            public int Count;
        }

        bool needsOptimize = false;
        List<MaterialCounter> counts;

		public void Initialize(ResourceManager cache)
        {
			Mesh.Initialize (StartMesh, endMesh, cache);
            ready = true;
            //Check if ref needs optimising
            if(endMesh - StartMesh > 5)
            {
                counts = new List<MaterialCounter>();
                for(int i = StartMesh; i < endMesh; i++) {
                    var crc = Mesh.Meshes[i].MaterialCrc;
                    bool add = true;
                    for(int j = 0; j < counts.Count; j++) {
                        if (counts[j].Material == crc)
                        {
                            counts[j].Count++;
                            add = false;
                            break;
                        }
                    }
                    if(add == true)
                        counts.Add(new MaterialCounter() { Count = 1, Material = crc });
                }
                foreach(var count in counts)
                {
                    if(count.Count > 1) {
                        needsOptimize = true;
                        break;
                    }
                }
                if (!needsOptimize) counts = null;
            }

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

		public void Draw(RenderState rstate, Matrix4 world, Lighting light, MaterialAnimCollection mc)
        {
            if (ready) Mesh.Draw(rstate, StartMesh, endMesh, StartVertex, world, light, mc);
        }

        class OptimizedDrawcall
        {
            public int StartIndex;
            public int VertexOffset;
            public int PrimitiveCount;
            public uint MaterialCrc;
            public ILibFile vMeshLibrary;
            private Material material;
            public Material Material
            {
                get
                {
                    if (material != null && !material.Loaded) material = null;
                    if (material == null) material = vMeshLibrary.FindMaterial(MaterialCrc);
                    return material;
                }
            }
        }
        class OptimizedDraw
        {
            public OptimizedDrawcall[] Optimized;
            public int[] NormalDraw;
        }
        class MaterialIndices
        {
            public uint Crc;
            public List<int> Indices = new List<int>();
        }
        OptimizedDraw optimized;
        void Optimize()
        {
            needsOptimize = false;
            List<int> meshes = new List<int>();
            bool didOptimise = false;
            List<MaterialIndices> mats = new List<MaterialIndices>();
            for(int i = StartMesh; i < endMesh; i++) {
                var c = counts.Where((x) => x.Material == Mesh.Meshes[i].MaterialCrc).First();
                if(c.Count == 1 || Mesh.Meshes[i].Material == null || Mesh.Meshes[i].Material.Render.IsTransparent) {
                    meshes.Add(i);
                } else {
                    didOptimise = true;
                    var m = mats.Where((x) => x.Crc == Mesh.Meshes[i].MaterialCrc).FirstOrDefault();
                    if (m == null) {
                        m = new MaterialIndices() { Crc = Mesh.Meshes[i].MaterialCrc };
                        mats.Add(m);
                    }
                    for (int j = Mesh.Meshes[i].TriangleStart; j < (Mesh.Meshes[i].TriangleStart + Mesh.Meshes[i].NumRefVertices); j++) {
                        m.Indices.Add(Mesh.Indices[j] + StartVertex + Mesh.Meshes[i].StartVertex);
                    }
                }
            }

            if (didOptimise)
            {
                List<OptimizedDrawcall> dcs = new List<OptimizedDrawcall>();
                List<ushort> indices = new List<ushort>();
                foreach (var m in mats)
                {
                    var dc = new OptimizedDrawcall();
                    dc.MaterialCrc = m.Crc;
                    dc.PrimitiveCount = m.Indices.Count / 3;
                    dc.StartIndex = indices.Count + Mesh.IndexHandle.CountIndex;
                    var min = m.Indices.Min();
                    for (int i = 0; i < m.Indices.Count; i++)
                        indices.Add((ushort)(m.Indices[i] - min));
                    dc.VertexOffset = min + Mesh.VertexOffset;
                    dc.vMeshLibrary = vMeshLibrary;
                    dcs.Add(dc);
                }
                if(Mesh.IndexHandle.CountIndex + indices.Count >= Mesh.IndexHandle.TotalIndex) {
                    FLLog.Warning("Vms", "Failed to optimise: Not enough space in element buffer");
                    return;
                }
                var arr = indices.ToArray();
                mesh.IndexHandle.Elements.SetData(arr, arr.Length, Mesh.IndexHandle.CountIndex);
                mesh.IndexHandle.CountIndex += arr.Length;
                FLLog.Debug("Optimiser", "Reduced from " + MeshCount + " drawcalls to " + (meshes.Count + dcs.Count));
                optimized = new OptimizedDraw();
                optimized.NormalDraw = meshes.ToArray();
                optimized.Optimized = dcs.ToArray();
            }
        }

        public void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting light, MaterialAnimCollection mc, Material overrideMat = null)
		{
            if (ready) {
                if(needsOptimize)
                    Optimize();
                if(optimized != null)
                {
                    for(int i = 0; i < optimized.Optimized.Length; i++)
                    {
                        var m = optimized.Optimized[i].Material;
                        if (m == null) m = vMeshLibrary.FindMaterial(0);
                        MaterialAnim ma = null;
                        if(mc != null) mc.Anims.TryGetValue(m.Name, out ma);
                        buffer.AddCommand(
                            m.Render,
                            ma,
                            world,
                            light,
                            Mesh.VertexBuffer,
                            PrimitiveTypes.TriangleList,
                            optimized.Optimized[i].VertexOffset,
                            optimized.Optimized[i].StartIndex,
                            optimized.Optimized[i].PrimitiveCount,
                            SortLayers.OBJECT,
                            0
                        );
                    }
                    for (int i = 0; i < optimized.NormalDraw.Length; i++)
                    {
                        Mesh.Meshes[optimized.NormalDraw[i]].DrawBuffer(buffer, Mesh, Mesh.VertexOffset, StartVertex, Mesh.IndexOffset, world, ref light, mc, overrideMat);
                    }

                } else
                    Mesh.DrawBuffer(buffer, StartMesh, endMesh, StartVertex, world, ref light, Center, mc, overrideMat);
            }
		}

		public void DepthPrepass(RenderState rstate, Matrix4 world, MaterialAnimCollection mc)
		{
            if (ready)
            {
                if (needsOptimize)
                    Optimize();
                if (optimized != null)
                {
                    for (int i = 0; i < optimized.Optimized.Length; i++)
                    {
                        var m = optimized.Optimized[i].Material;
                        if (m == null) m = vMeshLibrary.FindMaterial(0);
                        m.Render.World = world;
                        m.Render.ApplyDepthPrepass(rstate);
                        Mesh.VertexBuffer.Draw(PrimitiveTypes.TriangleList, optimized.Optimized[i].VertexOffset, optimized.Optimized[i].StartIndex, optimized.Optimized[i].PrimitiveCount);
                    }
                    for (int i = 0; i < optimized.NormalDraw.Length; i++)
                        Mesh.Meshes[optimized.NormalDraw[i]].DepthPrepass(rstate, Mesh, StartVertex + Mesh.VertexOffset, Mesh.IndexOffset, world, mc);
                }
                else
                    Mesh.DepthPrepass(rstate, StartMesh, endMesh, StartVertex, world, mc);
            }
		}

        public override string ToString()
        {
            return "VMeshRef";
        }
    }
}