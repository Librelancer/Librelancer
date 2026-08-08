using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;

namespace LibreLancer.Physics;

public struct SphereQuery
{
    public Vector3 Position;
    public float Radius;
    public bool Touched;
}

static class DynamicSpheresQuery
{
    public struct BatcherCallbacks : ICollisionCallbacks
    {
        public Buffer<bool> QueryWasTouched;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowCollisionTesting(int pairId, int childA, int childB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnChildPairCompleted(int pairId, int childA, int childB, ref ConvexContactManifold manifold)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnPairCompleted<TManifold>(int pairId, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            for (int i = 0; i < manifold.Count; ++i)
            {
                if (manifold.GetDepth(i) >= 0)
                {
                    QueryWasTouched[pairId] = true;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Called by the BroadPhase.GetOverlaps to collect all encountered collidables.
    /// </summary>
    struct BroadPhaseOverlapEnumerator : IBreakableForEach<CollidableReference>
    {
        public QuickList<CollidableReference> References;
        public PhysicsWorld World;
        //The enumerator never gets stored into unmanaged memory, so it's safe to include a reference type instance.
        public BufferPool Pool;
        public bool LoopBody(CollidableReference reference)
        {
            if (reference.Mobility == CollidableMobility.Static)
                return false;
            if (World.CollidableObjects[reference] == false)
                return false;
            References.Allocate(Pool) = reference;
            return true;
        }
    }
    static void GetPoseAndShape(Simulation sim, CollidableReference reference, out RigidPose pose, out TypedIndex shapeIndex)
    {
        //Collidables can be associated with either bodies or statics. We have to look in a different place depending on which it is.
        if (reference.Mobility == CollidableMobility.Static)
        {
            var collidable = sim.Statics[reference.StaticHandle];
            pose = collidable.Pose;
            shapeIndex = collidable.Shape;
        }
        else
        {
            var bodyReference = sim.Bodies[reference.BodyHandle];
            pose = bodyReference.Pose;
            shapeIndex = bodyReference.Collidable.Shape;
        }
    }

    static unsafe void AddQueryToBatch(Simulation sim, BufferPool pool, PhysicsWorld world, int queryShapeType, void* queryShapeData, int queryShapeSize, Vector3 queryBoundsMin, Vector3 queryBoundsMax, in RigidPose queryPose, int queryId, ref CollisionBatcher<BatcherCallbacks> batcher)
    {
        var broadPhaseEnumerator = new BroadPhaseOverlapEnumerator { Pool = pool, World = world, References = new QuickList<CollidableReference>(16, pool) };
        sim.BroadPhase.GetOverlaps(queryBoundsMin, queryBoundsMax, pool, ref broadPhaseEnumerator);
        for (int overlapIndex = 0; overlapIndex < broadPhaseEnumerator.References.Count; ++overlapIndex)
        {
            GetPoseAndShape(sim, broadPhaseEnumerator.References[overlapIndex], out var pose, out var shapeIndex);
            sim.Shapes[shapeIndex.Type].GetShapeData(shapeIndex.Index, out var shapeData, out _);
            //In this path, we assume that the incoming shape data is ephemeral. The collision batcher may last longer than the data pointer.
            //To avoid undefined access, we cache the query data into the collision batcher and use a pointer to the cache instead.
            batcher.CacheShapeB(shapeIndex.Type, queryShapeType, queryShapeData, queryShapeSize, out var cachedQueryShapeData);
            batcher.AddDirectly(
                shapeIndex.Type, queryShapeType,
                shapeData, cachedQueryShapeData,
                //Because we're using this as a boolean query, we use a speculative margin of 0. Don't care about negative depths.
                queryPose.Position - pose.Position, pose.Orientation, queryPose.Orientation, 0, new PairContinuation(queryId));
        }
        broadPhaseEnumerator.References.Dispose(pool);
    }

    static unsafe void AddQueryToBatch<TShape>(Simulation sim, BufferPool pool, PhysicsWorld world, TShape shape, in RigidPose pose, int queryId, ref CollisionBatcher<BatcherCallbacks> batcher) where TShape : IConvexShape
    {
        var queryShapeData = Unsafe.AsPointer(ref shape);
        var queryShapeSize = Unsafe.SizeOf<TShape>();
        shape.ComputeBounds(pose.Orientation, out var boundingBoxMin, out var boundingBoxMax);
        boundingBoxMin += pose.Position;
        boundingBoxMax += pose.Position;
        AddQueryToBatch(sim, pool, world, TShape.TypeId, queryShapeData, queryShapeSize, boundingBoxMin, boundingBoxMax, pose, queryId, ref batcher);
    }

    internal static void Run(Simulation simulation, BufferPool pool, QuickList<SphereQuery> queries,
        PhysicsWorld world)
    {
        var collisionBatcher = new CollisionBatcher<BatcherCallbacks>(pool, simulation.Shapes, simulation.NarrowPhase.CollisionTaskRegistry, 0, new BatcherCallbacks());
        ref var queryWasTouched = ref collisionBatcher.Callbacks.QueryWasTouched;
        pool.Take(queries.Count, out queryWasTouched);
        queryWasTouched.Clear(0, queryWasTouched.Length);


        for (int queryIndex = 0; queryIndex < queries.Count; ++queryIndex)
        {
            ref var query = ref queries[queryIndex];
            var sph = new Sphere(query.Radius);
            var pose = new RigidPose(query.Position, Quaternion.Identity);
            AddQueryToBatch(simulation, pool, world, sph, pose, queryIndex, ref collisionBatcher);
        }

        collisionBatcher.Flush();

        for (int i = 0; i < queries.Count; ++i)
        {
            queries[i].Touched = queryWasTouched[i];
        }
        pool.Return(ref queryWasTouched);
    }
}


