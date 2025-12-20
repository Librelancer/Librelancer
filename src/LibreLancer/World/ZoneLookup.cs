using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.World;

public class ZoneLookup : IDisposable
{
    private Tree tree;
    private BufferPool pool;

    private List<Zone> zones;

    static void ComputeBounds(Zone zone, out Vector3 min, out Vector3 max)
    {
        switch (zone.Shape)
        {
            case ShapeKind.Box:
            case ShapeKind.Ellipsoid:
                var b = new Box(zone.Size.X, zone.Size.Y, zone.Size.Z);
                b.ComputeBounds(zone.RotationMatrix.ExtractRotation(), out min, out max);
                break;
            case ShapeKind.Sphere:
                min = new Vector3(-zone.Size.X);
                max = new Vector3(zone.Size.X);
                break;
            case ShapeKind.Cylinder:
            case ShapeKind.Ring:
                var c = new Cylinder(zone.Size.X, zone.Size.Y);
                c.ComputeBounds(zone.RotationMatrix.ExtractRotation(), out min, out max);
                break;
            default:
                throw new ArgumentException();
        }
        min += zone.Position;
        max += zone.Position;
    }

    static void FillSubtreesForChildren(List<Zone> children, Span<NodeChild> subtrees)
    {
        for (int i = 0; i < children.Count; ++i)
        {
            ref var subtree = ref subtrees[i];
            ComputeBounds(children[i], out subtree.Min, out subtree.Max);
            subtree.LeafCount = 1;
            subtree.Index = Tree.Encode(i);
        }
    }

    public ZoneLookup(IEnumerable<Zone> z)
    {
        pool = new BufferPool();
        this.zones = z.ToList();
        RebuildTree();
    }

    unsafe void RebuildTree()
    {
        pool.Clear();
        if (zones.Count == 0)
            return;
        tree = new Tree(pool, zones.Count)
        {
            NodeCount = int.Max(1, zones.Count - 1),
            LeafCount = zones.Count
        };
        pool.Take(zones.Count, out Buffer<NodeChild> subtrees);
        FillSubtreesForChildren(zones, subtrees);
        tree.BinnedBuild(subtrees, pool);
        pool.Return(ref subtrees);
    }

    public void AddZone(Zone z)
    {
        zones.Add(z);
        RebuildTree();
    }

    public void RemoveZone(Zone z)
    {
        zones.Remove(z);
        RebuildTree();
    }

    private int frameIndex = 0;

    public void UpdatePositions()
    {
        RebuildTree();
        //FillSubtreesForChildren(zones, subtrees); - subtrees is invalid here
        //tree.RefitAndRefine(pool, frameIndex++);
    }

    struct PointIterator(Action<Zone> cb, ZoneLookup lookup, Vector3 pos) : IBreakableForEach<int>
    {
        public bool LoopBody(int i)
        {
            if (lookup.zones[i].ContainsPoint(pos))
                cb(lookup.zones[i]);
            return true;
        }
    }

    public void ZonesAtPosition(Vector3 position, Action<Zone> callback)
    {
        if (zones.Count == 0)
        {
            return;
        }
        var iterator = new PointIterator(callback, this, position);
        tree.GetOverlaps(position, position, ref iterator);
    }

    public void Dispose()
    {
        pool.Clear();
    }
}
