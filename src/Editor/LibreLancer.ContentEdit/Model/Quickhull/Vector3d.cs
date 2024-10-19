using System;
using System.Numerics;

namespace LibreLancer.ContentEdit.Model.Quickhull;

// Used in operations that must be double precision for accuracy
struct Vector3d(double x, double y, double z)
{
    public double X = x, Y = y, Z = z;

    public static explicit operator Vector3d(Vector3 v)
    {
        return new Vector3d(v.X, v.Y, v.Z);
    }

    public static readonly Vector3d Zero = new Vector3d(0, 0, 0);

    public static Vector3d Cross(Vector3d vector1, Vector3d vector2)
    {
        return new Vector3d(
            vector1.Y * vector2.Z -  vector1.Z *  vector2.Y,
            vector1.Z *  vector2.X -  vector1.X *  vector2.Z,
            vector1.X *  vector2.Y -  vector1.Y *  vector2.X);
    }

    public static double Dot(Vector3d vector1, Vector3d vector2)
    {
        return  ( vector1.X *  vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z);
    }

    public readonly double Length() => Math.Sqrt(this.LengthSquared());

    public readonly double LengthSquared() => Vector3d.Dot(this, this);

    public static Vector3d operator +(Vector3d v1, Vector3d v2)
    {
        return new Vector3d(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
    }

    public static Vector3d operator -(Vector3d v1, Vector3d v2)
    {
        return new Vector3d(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
    }

    public static Vector3d operator /(Vector3d v, double scalar)
    {
        return new Vector3d(v.X / scalar, v.Y / scalar, v.Z / scalar);
    }

    public static Vector3d operator *(Vector3d v, double scalar)
    {
        return new Vector3d(v.X * scalar, v.Y * scalar, v.Z * scalar);
    }


    public Vector3d Normalized() => this / Length();

    public void Normalize()
    {
        var l = Length();
        X /= l;
        Y /= l;
        Z /= l;
    }

    public override string ToString() => $"({X}, {Y}, {Z})";
}
