using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Render;
using LibreLancer.Utf.Vms;

namespace LibreLancer;

public struct VMeshOptimizeInfo
{

    public bool Enabled;

    public ushort StartMesh;
    public ushort EndMesh;
    public ushort StartVertex;

    public VMeshOptimizeInfo(bool enabled, ushort startMesh, ushort endMesh, ushort startVertex)
    {
        Enabled = enabled;
        StartMesh = startMesh;
        EndMesh = endMesh;
        StartVertex = startVertex;
    }

    public override int GetHashCode() => HashCode.Combine(StartMesh, EndMesh, StartVertex);

    public bool Equals(VMeshOptimizeInfo other)
    {
        return StartMesh == other.StartMesh && EndMesh == other.EndMesh && StartVertex == other.StartVertex && Enabled == other.Enabled;
    }

    public override bool Equals(object obj)
    {
        return obj is VMeshOptimizeInfo other && Equals(other);
    }

    public static bool operator ==(VMeshOptimizeInfo left, VMeshOptimizeInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VMeshOptimizeInfo left, VMeshOptimizeInfo right)
    {
        return !left.Equals(right);
    }

}

public class VMeshResource
{
    public bool IsDisposed => VertexResource?.IsDisposed ?? true;

    public VertexResource VertexResource;
    public TMeshHeader[] Meshes;
    public ushort[] Indices;

    private Dictionary<VMeshOptimizeInfo, (IndexResource, MeshDrawcall[])?> optimized;

    public (VMeshOptimizeInfo, MeshDrawcall[]) Optimize(ushort startMesh, ushort endMesh, ushort startVertex, ResourceManager resources)
    {
        var vmo = new VMeshOptimizeInfo(true, startMesh, endMesh, startVertex);
        if (endMesh - startMesh <= 3) return (new VMeshOptimizeInfo(), null);
        if (optimized != null && optimized.TryGetValue(vmo, out var v))
        {
            if (v == null)
                return (new VMeshOptimizeInfo(), null);
            else
                return (vmo, v.Value.Item2);
        }
        optimized ??= new Dictionary<VMeshOptimizeInfo, (IndexResource, MeshDrawcall[])?>();
        //Check material counts
        var counts = new List<(uint Material, int Count)>();
        for(int i = startMesh; i < endMesh; i++) {
            var crc = Meshes[i].MaterialCrc;
            bool add = true;
            for(int j = 0; j < counts.Count; j++) {
                if (counts[j].Material == crc)
                {
                    counts[j] = (counts[j].Material, counts[j].Count + 1);
                    add = false;
                    break;
                }
            }
            if(add)
                counts.Add((crc, 1));
        }
        if (!counts.Any(x => x.Count > 1)){
            optimized[vmo] = null;
            return (new VMeshOptimizeInfo(), null);
        }
        //Optimize
        List<MeshDrawcall> drawcalls = new List<MeshDrawcall>();
        List<(uint MaterialCrc, List<int> Indices)> merged = new List<(uint MaterialCrc, List<int> Indices)>();
        for (int i = startMesh; i < endMesh; i++)
        {
            var c = counts.First(x => x.Material == Meshes[i].MaterialCrc);
            var mat = resources.FindMaterial(c.Material);
            if (c.Count == 1 || mat == null || mat.Render.IsTransparent){
                drawcalls.Add(new MeshDrawcall()
                {
                    BaseVertex = Meshes[i].StartVertex + startVertex,
                    MaterialCrc = Meshes[i].MaterialCrc,
                    PrimitiveCount = Meshes[i].NumRefVertices / 3,
                    StartIndex = Meshes[i].TriangleStart
                });
            }
            else
            {
                var m = merged.FirstOrDefault(x => x.MaterialCrc == Meshes[i].MaterialCrc);
                if (m.Indices == null)
                {
                    m = (Meshes[i].MaterialCrc, new List<int>());
                    merged.Add(m);
                }
                for (int j = Meshes[i].TriangleStart; j < (Meshes[i].TriangleStart + Meshes[i].NumRefVertices); j++) {
                    m.Indices.Add(Indices[j] + startVertex + Meshes[i].StartVertex);
                }
            }
        }
        if (merged.Count == 0)
        {
            optimized[vmo] = null;
            return (new VMeshOptimizeInfo(), null);
        }
        var indexBuffer = new List<int>();
        foreach(var b in  merged)
            indexBuffer.AddRange(b.Indices);
        var baseVertex = indexBuffer.Min();
        var newIndices = indexBuffer.Select(x => checked ((ushort) (x - baseVertex))).ToArray();
        var indexResource = VertexResource.Allocator.AllocateIndex(newIndices);
        int startIndex = indexResource.StartIndex - VertexResource.StartIndex;
        foreach (var b in merged) {
            drawcalls.Add(new MeshDrawcall()
            {
                BaseVertex = baseVertex,
                MaterialCrc = b.MaterialCrc,
                PrimitiveCount = b.Indices.Count / 3,
                StartIndex = startIndex
            });
            startIndex += b.Indices.Count;
        }
        var newCalls = drawcalls.ToArray();
        FLLog.Debug("Optimiser", $"Reduced from {(endMesh - startMesh)} drawcalls to {(drawcalls.Count)}");
        optimized[vmo] = (VertexResource.Allocator.AllocateIndex(newIndices), newCalls);
        return (vmo, newCalls);
    }

    public void OptimizeIfNeeded(VMeshOptimizeInfo info, ResourceManager resources)
    {
        if (!info.Enabled || optimized == null) return;
        if (!optimized.TryGetValue(info, out var v)){
            Optimize(info.StartMesh, info.EndMesh, info.StartVertex, resources);
            v = optimized[info];
        }
    }

    public void Dispose()
    {
        VertexResource.Dispose();
        if (optimized != null)
        {
            foreach (var v in optimized.Values)
                v?.Item1.Dispose();
        }
        optimized = null;
    }
}
