using System.Numerics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render;

public class DfmHardpoint : IRenderHardpoint
{
    public DfmSkeletonManager.Connection Connection;
    public BoneInstance Bone;
    public HardpointDefinition Definition;

    public Matrix4x4 Transform => Definition.Transform *
                                  (Bone?.LocalTransform ?? Matrix4x4.Identity) *
                                  (Connection?.Transform ?? Matrix4x4.Identity);
}
