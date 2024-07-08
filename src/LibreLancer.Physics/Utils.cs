using System.Numerics;
using BepuPhysics;

namespace LibreLancer.Physics;

static class Utils
{
    public static RigidPose ToPose(this Transform3D mat) => new(mat.Position, Quaternion.Normalize(mat.Orientation));
}
