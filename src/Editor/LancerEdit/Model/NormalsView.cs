using System;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;

namespace LancerEdit;

public class NormalsView : IDisposable
{
    private ResourceManager res;
    private Dictionary<(string Name, int Level), (int Start, int Count)> lines = new();
    public VertexBuffer VertexBuffer;

    public NormalsView(RenderContext context, IDrawable drawable, ResourceManager res, float len)
    {
        this.res = res;
        var verts = new List<VertexPositionColor>();
        if (drawable is ModelFile mf)
        {
            Process("ROOT", mf, verts, len);
        }
        else if (drawable is CmpFile cmp)
        {
            foreach (var p in cmp.ModelParts())
            {
                Process(p.ObjectName, p.Model, verts, len);
            }
        }

        if (verts.Count > 0)
        {
            VertexBuffer = new VertexBuffer(context, typeof(VertexPositionColor), verts.Count);
            VertexBuffer.SetData<VertexPositionColor>(verts.ToArray());
        }
    }

    public bool TryGet(string name, int level, out int start, out int count)
    {
        start = count = 0;
        if (lines.TryGetValue((name, level), out var line))
        {
            start = line.Start;
            count = line.Count;
            return true;
        }
        return false;
    }

    void Process(string name, ModelFile mf, List<VertexPositionColor> verts, float len)
    {
        for (int lidx = 0; lidx < mf.Levels.Length; lidx++)
        {
            var lvl = mf.Levels[lidx];
            int start = verts.Count;
            var vms = res.FindMeshData(lvl.MeshCrc);
            if (!vms.VertexFormat.Normal)
                continue;
            for (int t = lvl.StartMesh; t < lvl.StartMesh + lvl.MeshCount; t++)
            {
                var tmesh = vms.Meshes[t];
                var baseVertex = lvl.StartVertex + tmesh.StartVertex;
                int indexStart = tmesh.TriangleStart;
                int indexCount = tmesh.NumRefVertices;
                for (int i = indexStart; i < indexStart + indexCount; i++)
                {
                    var idx = baseVertex + vms.Indices[i];
                    var p = vms.GetPosition(idx);
                    var n = vms.GetNormal(idx);
                    var c1 = new Color4(
                        -MathHelper.Clamp(n.X, -1, 0),
                        -MathHelper.Clamp(n.Y, -1, 0),
                        -MathHelper.Clamp(n.Z, -1, 0),
                        1
                    );
                    var c2 = new Color4(
                        MathHelper.Clamp(n.X, 0, 1),
                        MathHelper.Clamp(n.Y, 0, 1),
                        MathHelper.Clamp(n.Z, 0, 1),
                        1
                    );
                    verts.Add(new(p, c1));
                    verts.Add(new(p + n * len, c2));
                }
            }
            if (verts.Count - start > 0)
            {
                lines[(name, lidx)] = (start, verts.Count - start);
            }
        }

    }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
    }
}
