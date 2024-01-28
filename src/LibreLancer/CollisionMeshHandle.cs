using LibreLancer.Sur;

namespace LibreLancer;

public struct CollisionMeshHandle
{
    public SurFile Sur;
    public uint FileId;

    public bool Valid => Sur != null && FileId != 0;
}
