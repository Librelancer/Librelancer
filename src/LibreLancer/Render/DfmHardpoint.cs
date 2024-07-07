using System.Numerics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render;

public class DfmHardpoint : IRenderHardpoint
{
    public DfmSkeletonManager.Connection Connection;
    public BoneInstance Bone;
    public HardpointDefinition Definition;

    public Transform3D Transform => Definition.Transform *
                                  (Bone?.LocalTransform ?? Transform3D.Identity) *
                                  (Connection?.Transform ?? Transform3D.Identity);
}
