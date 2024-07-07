using System.Numerics;

namespace LibreLancer;

public record struct Transform3D(Vector3 Position, Quaternion Orientation)
{
    public static readonly Transform3D Identity = new (Vector3.Zero, Quaternion.Identity);

    public static Transform3D FromMatrix(Matrix4x4 matrix) =>
        new(Vector3.Transform(Vector3.Zero, matrix), matrix.ExtractRotation());

    public readonly Vector3 Transform(Vector3 position) =>
        Vector3.Transform(position, Orientation) + Position;

    public readonly Vector3 InverseTransform(Vector3 position) =>
        Vector3.Transform(position - Position, Quaternion.Conjugate(Orientation));

    public readonly Matrix4x4 Matrix() =>
        Matrix4x4.CreateFromQuaternion(Orientation) * Matrix4x4.CreateTranslation(Position);

    public readonly Transform3D Inverse()
    {
        var o = Quaternion.Inverse(Orientation);
        var p = Vector3.Transform(-Position, o);
        return new Transform3D(p, o);
    }

    public static Transform3D operator *(Transform3D a, Transform3D b) =>
        new(Vector3.Transform(a.Position, b.Orientation) + b.Position,
            Quaternion.Concatenate(a.Orientation, b.Orientation));

    public readonly Vector3 GetEulerDegrees() => Matrix4x4.CreateFromQuaternion(Orientation).GetEulerDegrees();
}
