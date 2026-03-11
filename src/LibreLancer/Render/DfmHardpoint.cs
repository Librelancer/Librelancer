using System.Numerics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render;

public class DfmHardpoint : IRenderHardpoint
{
    public DfmSkeletonManager.Connection? Connection;
    public BoneInstance Bone;
    public HardpointDefinition Definition;

    public DfmHardpoint(DfmSkeletonManager.Connection? connection, BoneInstance bone, HardpointDefinition definition)
    {
        Connection = connection;
        Bone = bone;
        Definition = definition;
    }

    public Transform3D Transform => Definition.Transform *
                                    (Bone?.LocalTransform ?? Transform3D.Identity) *
                                    (Connection?.Transform ?? Transform3D.Identity);
}
