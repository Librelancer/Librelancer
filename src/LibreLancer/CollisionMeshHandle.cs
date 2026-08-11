using LibreLancer.Sur;

namespace LibreLancer;

public struct CollisionMeshHandle
{
    public uint FileId;

    public bool Valid => FileId != 0;
}
