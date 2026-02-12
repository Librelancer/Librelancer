using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LibreLancer.Physics;

public static class PhysicsWarmup
{
    public static void Warmup() => RuntimeHelpers.RunClassConstructor(typeof(PhysicsWarmup).TypeHandle);
    static PhysicsWarmup() => RunWarmup();

    class WarmupMeshes : IConvexMeshProvider
    {
        Dictionary<uint, ConvexMesh[]> meshes = new();

        static readonly Vector3[] cubeVertices = new Vector3[]
        {
            new( 1,  1, -1), new( 1, -1, -1),
            new( 1,  1,  1), new( 1, -1,  1),
            new(-1,  1, -1), new(-1, -1, -1),
            new(-1,  1,  1), new(-1, -1,  1)
        };
        private static readonly int[] cubeIndices = new int[] {
            0, 4, 6, 3, 2, 6, 7, 6, 4, 5, 1, 3, 1, 0, 2, 5, 4, 0, 6, 2, 0,
            6, 7, 3, 4, 5, 7, 3, 7, 5, 2, 3, 1, 0, 1, 5
        };

        public bool HasShape(uint meshId) => true;

        public ConvexMesh[] GetMesh(ConvexMeshId meshId) =>
            [new () { Indices = cubeIndices, Vertices = cubeVertices }];
    }

    static void RunWarmup()
    {
        var sw = Stopwatch.StartNew();
        using var collection = new ConvexMeshCollection(_ => new WarmupMeshes());
        using var world = new PhysicsWorld(collection);
        world.OnCollision += WorldOnOnCollision;
        var fileId = collection.UseFile("file");

        bool anyCollided = false;
        bool sphereCollided = false;

        void WorldOnOnCollision(PhysicsObject objA, PhysicsObject objB)
        {
            anyCollided = true;
            if (objA.Collider is SphereCollider || objB.Collider is SphereCollider)
                sphereCollided = true;
        }

        using var cubeCollider0 = new ConvexMeshCollider(world);
        cubeCollider0.AddPart(fileId, default, Transform3D.Identity, null);
        using var cubeCollider1 = new ConvexMeshCollider(world);
        cubeCollider1.AddPart(fileId, default, Transform3D.Identity, null);
        using var cubeCollider2 = new ConvexMeshCollider(world);
        cubeCollider2.AddPart(fileId, default, Transform3D.Identity, null);
        using var sphereCollider = new SphereCollider(2);

        using var cube0 = world.AddDynamicObject(8, Transform3D.Identity, cubeCollider0);
        using var cube1 = world.AddDynamicObject(8,
            new Transform3D(new(-0.25f, -0.25f, -100), Quaternion.Identity), cubeCollider1);

        using var cube2 = world.AddDynamicObject(8,
            new Transform3D(new(-0.25f, 100f, -100f), Quaternion.Identity), cubeCollider2);
        using var sphere1 = world.AddStaticObject(
            new Transform3D(new(0, 100, 0), Quaternion.Identity), sphereCollider);


        for (int i = 0; i < 500; i++)
        {
            cube1.AddForce(new(0, 0, 50));
            cube2.AddForce(new(0, 0, 50));
            world.StepSimulation(1 / 60.0f);
        }

        sw.Stop();
        FLLog.Debug("Physics", $"Warmup took {sw.Elapsed.TotalMilliseconds}ms");
        if (!anyCollided || !sphereCollided)
            throw new Exception("Physics warmup failed");
    }
}
