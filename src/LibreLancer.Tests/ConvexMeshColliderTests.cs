using System.Numerics;
using LibreLancer.Physics;
using Xunit;

namespace LibreLancer.Tests;

public class ConvexMeshColliderTests
{
    private sealed class TetrahedronProvider : IConvexMeshProvider
    {
        public bool HasShape(uint meshId) => true;

        public ConvexMesh[] GetMesh(ConvexMeshId meshId) =>
        [
            new ConvexMesh
            {
                Vertices =
                [
                    new Vector3(-1, -1, -1),
                    new Vector3(1, -1, -1),
                    new Vector3(0, 1, -1),
                    new Vector3(0, 0, 1)
                ],
                Indices =
                [
                    0, 2, 1,
                    0, 1, 3,
                    1, 2, 3,
                    2, 0, 3
                ]
            }
        ];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovingCompoundPartPreservesRemainingRaycastTags(bool removeAfterRegistration)
    {
        using var meshes = new ConvexMeshCollection(_ => new TetrahedronProvider());
        using var world = new PhysicsWorld(meshes);
        using var collider = new ConvexMeshCollider(world);
        var provider = meshes.UseFile("tag-test");
        var firstTag = new object();
        var middleTag = new object();
        var lastTag = new object();

        Assert.True(collider.AddPart(provider, new ConvexMeshId(1, 0),
            new Transform3D(new Vector3(-5, 0, 0), Quaternion.Identity), firstTag));
        Assert.True(collider.AddPart(provider, new ConvexMeshId(2, 0),
            Transform3D.Identity, middleTag));
        Assert.True(collider.AddPart(provider, new ConvexMeshId(3, 0),
            new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity), lastTag));

        if (removeAfterRegistration)
            world.AddStaticObject(Transform3D.Identity, collider);

        collider.RemovePart(firstTag);

        if (!removeAfterRegistration)
            world.AddStaticObject(Transform3D.Identity, collider);

        collider.FinishUpdatePart();

        Assert.True(world.PointRaycast(null, new Vector3(0, 0, -10), Vector3.UnitZ, 20,
            out _, out _, out var hitTag));
        Assert.Same(middleTag, hitTag);

        Assert.True(world.PointRaycast(null, new Vector3(5, 0, -10), Vector3.UnitZ, 20,
            out _, out _, out hitTag));
        Assert.Same(lastTag, hitTag);
    }
}
