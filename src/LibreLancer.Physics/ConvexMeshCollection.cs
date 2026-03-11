using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace LibreLancer.Physics;

internal class ConvexMeshItem
{
    public required (ConvexHull Hull, Vector3 Center)[] Hulls;
    public required Triangle[] Triangles;
}

public class ConvexMeshCollection(Func<string, IConvexMeshProvider> factory) : IDisposable
{
    private readonly BufferPool pool = new BufferPool();
    private readonly Lock poolLock = new Lock();
    private readonly Lock filesLock = new Lock();

    private readonly ConcurrentDictionary<ShapeId, Lazy<ConvexMeshItem>> shapes = new();
    private readonly ConcurrentDictionary<ulong, Lazy<IConvexMeshProvider>> files = new();

    private static uint Hash(string s)
    {
        var num = 0x811c9dc5;
        foreach (var character in s.Select(character => (int) character))
        {
            var i = character;
            if (i is >= 65 and <= 90)
            {
                i ^= (1 << 5);
            }

            num = ((uint)i ^ num) * 0x1000193;
        }
        return num;
    }

    private IConvexMeshProvider LoadFile(string filename)
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

    public void CreateShape(uint fileId, ConvexMeshId meshId) => GetShapes(fileId, meshId);


    internal ConvexMeshItem GetShapes(uint fileId, ConvexMeshId meshId) =>
        shapes.GetOrAdd(
            meshId.ShapeId(fileId),
            _ => new Lazy<ConvexMeshItem>(() => Create(fileId, meshId))
            ).Value;

    private ConvexMeshItem Create(uint fileId, ConvexMeshId meshId)
    {
        if (!files.TryGetValue(fileId, out var f))
        {
            throw new InvalidOperationException("File has not been added to collection");
        }

        var src = f.Value.GetMesh(meshId);
        var hulls = new List<(ConvexHull Hull, Vector3 Center)>();
        var tris = new List<Triangle>();
        for (var i = 0; i < src.Length; i++)
        {
            var verts = src[i].Vertices;
            var indices = src[i].Indices;
            var points = new Vector3[src[i].Indices.Length];
            for (var j = 0; j < indices.Length; j++)
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

            for (var j = 0; j < indices.Length; j += 3)
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
