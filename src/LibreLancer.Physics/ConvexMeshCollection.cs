using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace LibreLancer.Physics;

class ConvexMeshItem
{
    public (ConvexHull Hull, Vector3 Center)[] Hulls;
    public Triangle[] Triangles;
}

public class ConvexMeshCollection : IDisposable
{
    private BufferPool pool = new BufferPool();
    private object poolLock = new object();
    private object filesLock = new object();

    private ConcurrentDictionary<ulong, Lazy<ConvexMeshItem>> shapes = new();
    private ConcurrentDictionary<ulong, Lazy<IConvexMeshProvider>> files = new();

    static uint Hash(string s)
    {
        uint num = 0x811c9dc5;
        for (int i = 0; i < s.Length; i++)
        {
            var c = (int) s[i];
            if ((c >= 65 && c <= 90))
                c ^= (1 << 5);
            num = ((uint)c ^ num) * 0x1000193;
        }
        return num;
    }

    private Func<string, IConvexMeshProvider> factory;

    public ConvexMeshCollection(Func<string, IConvexMeshProvider> factory)
    {
        this.factory = factory;
    }

    IConvexMeshProvider LoadFile(string filename)
    {
        lock (filesLock)
        {
            return factory(filename);
        }
    }

    public uint UseFile(string filename)
    {
        var id = Hash(filename);
        files.TryAdd(id, new Lazy<IConvexMeshProvider>(() => LoadFile(filename)));
        return id;
    }

    public void CreateShape(uint fileId, uint meshId) => GetShapes(fileId, meshId);

    internal ConvexMeshItem GetShapes(uint fileId, uint meshId) =>
        shapes.GetOrAdd(
            (ulong) meshId | ((ulong) fileId << 32),
            _ => new Lazy<ConvexMeshItem>(() => Create(fileId, meshId))
            ).Value;

    ConvexMeshItem Create(uint fileId, uint meshId)
    {
        if (!files.TryGetValue(fileId, out var f))
            throw new InvalidOperationException("File has not been added to collection");
        var src = f.Value.GetMesh(meshId);
        var hulls = new List<(ConvexHull Hull, Vector3 Center)>();
        var tris = new List<Triangle>();
        for (int i = 0; i < src.Length; i++)
        {
            var verts = src[i].Vertices;
            var indices = src[i].Indices;
            var points = new Vector3[src[i].Indices.Length];
            for (int j = 0; j < indices.Length; j++)
                points[j] = verts[indices[j]];
            lock (poolLock)
            {
                if (ConvexHullHelper.CreateShape(points, pool, out var center, out var convexHull))
                {
                    if (convexHull.FaceToVertexIndicesStart.Length <= 2)
                    {
                        convexHull.Dispose(pool);
                    }
                    else
                    {
                        hulls.Add((convexHull, center));
                        continue;
                    }
                }
            }

            for (int j = 0; j < indices.Length; j += 3)
            {
                tris.Add(new Triangle(verts[indices[j]], verts[indices[j + 1]], verts[indices[j + 2]]));
            }
        }
        return new ConvexMeshItem() {Hulls = hulls.ToArray(), Triangles = tris.ToArray()};
    }

    public void Dispose()
    {
        pool.Clear();
    }

}
