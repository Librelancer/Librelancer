using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Trees;
using BepuUtilities.Collections;
using BepuUtilities.Memory;

namespace LibreLancer.Physics;

public struct RayQuery
{
    public RayQuery(Vector3 origin, Vector3 direction, float maxT)
    {
        Origin = origin;
        Direction = direction;
        MaximumT = maxT;
        Hit = false;
    }
    public Vector3 Origin;
    public float MaximumT;
    public Vector3 Direction;
    public bool Hit;
    public float HitT;
}

static class BatchedRaycastQuery
{
    struct RayHit
    {
        public float T;
        public CollidableReference Collidable;
        public bool Hit;
    }

    unsafe struct HitHandler : IRayHitHandler
    {
        public Buffer<RayHit> Hits;
        public PhysicsWorld World;
        public int SelfId;
        public CollisionKind Allowed;

        //public int* IntersectionCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
            => World.bepuToLancer[collidable] != SelfId &&
               World.CollidableObjects[collidable] <= Allowed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
            => AllowTest(collidable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            maximumT = t;
            ref var hit = ref Hits[ray.Id];
            if (t < hit.T)
            {
                hit.T = t;
                hit.Collidable = collidable;
                hit.Hit = true;
            }
        }
    }

    public static void BatchedRaycast(PhysicsObject? me, QuickList<RayQuery> rays,
        PhysicsObject?[] hitObjects,
        PhysicsWorld world,
        bool allowDynAsteroids)
    {
        world.BufferPool.Take(rays.Count, out Buffer<RayHit> hits);
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].T = float.MaxValue;
            hits[i].Hit = false;
        }

        var hitHandler = new HitHandler
        {
            Hits = hits, World = world, SelfId = me?.Id ?? -1,
            Allowed = allowDynAsteroids ? CollisionKind.DynAsteroid : CollisionKind.StaticBody
        };
        var batcher = new SimulationRayBatcher<HitHandler>(world.BufferPool, world.Simulation, hitHandler, rays.Count);
        for (int i = 0; i < rays.Count; i++)
        {
            ref var ray = ref rays[i];
            batcher.Add(ref ray.Origin, ref ray.Direction, ray.MaximumT, i);
        }
        batcher.Flush();
        batcher.Dispose();

        for (int i = 0; i < rays.Count; i++)
        {
            rays[i].Hit = hits[i].Hit;
            if (rays[i].Hit)
            {
                rays[i].HitT = hits[i].T;
                hitObjects[i] = (world.objectsById[world.bepuToLancer[hits[i].Collidable]]);
            }
        }
        world.BufferPool.Return(ref hits);
    }
}
