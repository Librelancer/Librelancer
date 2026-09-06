using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace LibreLancer.Physics;

internal class ConvexShapeItem
{
    public required (ConvexHull Hull, Vector3 Center)[] Hulls;
    public required Triangle[] Triangles;
}

public class ConvexShapeCollection(Func<string, IConvexShapeProvider> factory) : IDisposable
{
    private readonly BufferPool pool = new BufferPool();
    private readonly Lock poolLock = new Lock();
    private readonly Lock filesLock = new Lock();

    private readonly ConcurrentDictionary<ShapeId, Lazy<ConvexShapeItem>> shapes = new();
    private readonly ConcurrentDictionary<ulong, Lazy<IConvexShapeProvider>> files = new();

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

    private IConvexShapeProvider LoadFile(string filename)
    {
        lock (filesLock)
        {
            return factory(filename);
        }
    }

    public uint UseFile(string filename)
    {
        var id = Hash(filename);
        files.TryAdd(id, new Lazy<IConvexShapeProvider>(() => LoadFile(filename)));
        return id;
    }

    public void CreateShape(uint fileId, ConvexShapeId shapeId) => GetShapes(fileId, shapeId);


    internal ConvexShapeItem GetShapes(uint fileId, ConvexShapeId shapeId) =>
        shapes.GetOrAdd(
            shapeId.ShapeId(fileId),
            _ => new Lazy<ConvexShapeItem>(() => Create(fileId, shapeId))
            ).Value;

    private ConvexShapeItem Create(uint fileId, ConvexShapeId shapeId)
    {
        if (!files.TryGetValue(fileId, out var f))
        {
            throw new InvalidOperationException("File has not been added to collection");
        }

        var src = f.Value.GetShape(shapeId);
        var hulls = new List<(ConvexHull Hull, Vector3 Center)>();
        var tris = new List<Triangle>();
        for (var i = 0; i < src.Length; i++)
        {
            if (src[i].IsHull)
            {
                lock (poolLock)
                {
                    hulls.Add(src[i].CreateHull(pool));
                }
            }
            else
            {
                tris.AddRange(src[i].GetTriangles());
            }
        }
        return new ConvexShapeItem() {Hulls = hulls.ToArray(), Triangles = tris.ToArray()};
    }

    public void Dispose()
    {
        pool.Clear();
    }

}
