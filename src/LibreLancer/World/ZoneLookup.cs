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
    private bool isDisposed = false;

    public const float PopulationZoneVisitDistance = 5_000f;
    private const float SizeModifierZoneVisit = PopulationZoneVisitDistance + 200f;

    private Tree tree;
    private BufferPool pool;

    private Tree farLookupTree;

    private List<Zone> zones;

    private static void ComputeBounds(Zone zone, float sizeMod, out Vector3 min, out Vector3 max)
    {
        var sz = zone.Size + new Vector3(sizeMod);
        switch (zone.Shape)
        {
            case ShapeKind.Box:
                var b = new Box(sz.X, sz.Y, sz.Z);
                b.ComputeBounds(zone.RotationMatrix.ExtractRotation(), out min, out max);
                break;
            case ShapeKind.Ellipsoid:
                var sz2 = zone.Size * 2 + new Vector3(sizeMod);
                var b2 = new Box(sz2.X, sz2.Y, sz2.Z);
                b2.ComputeBounds(zone.RotationMatrix.ExtractRotation(), out min, out max);
                break;
            case ShapeKind.Sphere:
                min = new Vector3(-sz.X);
                max = new Vector3(sz.X);
                break;
            case ShapeKind.Cylinder:
            case ShapeKind.Ring:
                var c = new Cylinder(sz.X, sz.Y);
                c.ComputeBounds(zone.RotationMatrix.ExtractRotation(), out min, out max);
                break;
            default:
                throw new ArgumentException();
        }
        min += zone.Position;
        max += zone.Position;
    }

    private static void FillSubtreesForChildren(List<Zone> children, Span<NodeChild> subtrees, float sizeMod)
    {
        for (int i = 0; i < children.Count; ++i)
        {
            ref var subtree = ref subtrees[i];
            ComputeBounds(children[i], sizeMod, out subtree.Min, out subtree.Max);
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

    private unsafe void RebuildTree()
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
        FillSubtreesForChildren(zones, subtrees, 0);
        tree.BinnedBuild(subtrees, pool);
        pool.Return(ref subtrees);

        farLookupTree = new Tree(pool, zones.Count)
        {
            NodeCount = int.Max(1, zones.Count - 1),
            LeafCount = zones.Count
        };

        pool.Take(zones.Count, out subtrees);
        FillSubtreesForChildren(zones, subtrees, SizeModifierZoneVisit);
        farLookupTree.BinnedBuild(subtrees, pool);
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
    }

    private struct PointIterator(Action<Zone> cb, ZoneLookup lookup, Vector3 pos) : IBreakableForEach<int>
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
        tree.GetOverlaps(new BepuUtilities.BoundingBox(position, position), pool, ref iterator);
    }

    private struct DistanceIterator(Action<Zone, bool> cb, ZoneLookup lookup, Vector3 pos) : IBreakableForEach<int>
    {
        public bool LoopBody(int i)
        {
            var zone = lookup.zones[i];
            var near = zone.DistanceToEdge(pos) <= PopulationZoneVisitDistance;
            var contains = zone.ContainsPoint(pos);
            if (near || contains)
                cb(zone, contains);
            return true;
        }
    }

    /// <summary>
    /// Returns a list of zones where the given position is 5k from the zones edge.
    /// </summary>
    /// <param name="position">The position to query from</param>
    /// <param name="callback">Callback with the nearby zone and whether or not the point is inside it</param>
    public void NearbyZones(Vector3 position, Action<Zone, bool> callback)
    {
        if (zones.Count == 0)
        {
            return;
        }
        var iterator = new DistanceIterator(callback, this, position);
        tree.GetOverlaps(new BepuUtilities.BoundingBox(position, position), pool, ref iterator);
    }

    public void Dispose()
    {
        pool.Clear();
        isDisposed = true;
    }

    #if DEBUG
    ~ZoneLookup()
    {
        Debug.Assert(isDisposed, "Memory leak warning! Don't let a ZoneLookup die without disposing!");
    }
    #endif
}
