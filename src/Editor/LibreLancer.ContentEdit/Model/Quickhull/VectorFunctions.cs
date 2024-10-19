using System;
using System.Numerics;

namespace LibreLancer.ContentEdit.Model.Quickhull;

static class VectorFunctions
{
    public static float PointLineDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        var ap = p - a;
        var area = Vector3.Cross(ap, ab).LengthSquared();
        var s = ab.LengthSquared();
        if (Math.Abs(s) < float.Epsilon)
            throw new Exception("a and b are the same point");
        return MathF.Sqrt(area / s);
    }
    public static Vector3 PlaneNormal(Vector3 point1, Vector3 point2, Vector3 point3) =>
        Vector3.Cross((point1 - point2), (point2 - point3)).Normalized();

    public static Vector3d PlaneNormal(Vector3d point1, Vector3d point2, Vector3d point3) =>
        Vector3d.Cross((point1 - point2), (point2 - point3)).Normalized();
}
