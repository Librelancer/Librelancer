using System.Numerics;
using BepuPhysics;

namespace LibreLancer.Physics;

static class Utils
{
    public static RigidPose ToPose(this Matrix4x4 mat)
    {
        var p = Vector3.Transform(Vector3.Zero, mat);
        var q = Quaternion.Normalize(mat.ExtractRotation());
        return new RigidPose(p, q);
    }

    public static Matrix4x4 ToMatrix(this RigidPose pose)
    {
        return Matrix4x4.CreateFromQuaternion(pose.Orientation) *
               Matrix4x4.CreateTranslation(pose.Position);
    }
}
