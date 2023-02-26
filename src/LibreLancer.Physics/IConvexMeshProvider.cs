namespace LibreLancer.Physics;

public interface IConvexMeshProvider
{
    bool HasShape(uint meshId);
    ConvexMesh[] GetMesh(uint meshId);
}