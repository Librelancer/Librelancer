using System.Numerics;
using LibreLancer.Physics;
using Xunit;

namespace LibreLancer.Tests;

public class ConvexMeshColliderTests
{
    private static readonly object PartTag = new();

    private sealed class MeshProvider : IConvexMeshProvider
    {
        private static readonly Vector3[] Vertices =
        [
            new(-10, -20, -100), new(10, -20, -100), new(-10, 20, -100), new(10, 20, -100),
            new(-10, -20, 100), new(10, -20, 100), new(-10, 20, 100), new(10, 20, 100)
        ];

        private static readonly int[] Indices =
        [
            0, 2, 3, 0, 3, 1,
            4, 5, 7, 4, 7, 6,
            0, 1, 5, 0, 5, 4,
            2, 6, 7, 2, 7, 3,
            0, 4, 6, 0, 6, 2,
            1, 3, 7, 1, 7, 5
        ];

        public bool HasShape(uint meshId) => true;

        public ConvexMesh[] GetMesh(ConvexMeshId meshId) =>
            [new ConvexMesh { Vertices = Vertices, Indices = Indices }];
    }

    [Fact]
    public void RadiusUsesActualConvexMeshExtent()
    {
        using var collection = new ConvexMeshCollection(_ => new MeshProvider());
        using var world = new PhysicsWorld(collection);
        using var collider = new ConvexMeshCollider(world);
        var fileId = collection.UseFile("elongated-test-mesh");

        Assert.True(collider.AddPart(fileId, default, Transform3D.Identity, null));
        Assert.True(collider.Radius >= 100, $"Expected an elongated radius, got {collider.Radius}");

        using var body = world.AddDynamicObject(1, Transform3D.Identity, collider);
    }

    [Fact]
    public void RadiusTracksPartMovementAndRemoval()
    {
        using var collection = new ConvexMeshCollection(_ => new MeshProvider());
        using var world = new PhysicsWorld(collection);
        using var collider = new ConvexMeshCollider(world);
        var fileId = collection.UseFile("moving-test-mesh");

        Assert.True(collider.AddPart(fileId, default, Transform3D.Identity, PartTag));
        using var body = world.AddDynamicObject(1, Transform3D.Identity, collider);

        collider.UpdatePart(PartTag, new Transform3D(new Vector3(500, 0, 0), Quaternion.Identity));
        Assert.True(collider.Radius >= 500, $"Expected moved part in radius, got {collider.Radius}");

        collider.RemovePart(PartTag);
        Assert.Equal(1, collider.Radius);
    }
}
