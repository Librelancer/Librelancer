using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace LibreLancer.Sur;

public class SurfaceNode
{
    public Vector3 Center;
    public float Radius;
    public Vector3 Scale;
    public byte Unknown;

    public SurfaceHull Hull;
    public SurfaceNode Left;
    public SurfaceNode Right;

    public static SurfaceNode Read(BinaryReader reader)
    {
        return new SurfaceNode()
        {
            Center = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Radius = reader.ReadSingle(),
            Scale = new Vector3(reader.ReadByte(), reader.ReadByte(), reader.ReadByte()) / 0xFA,
            Unknown = reader.ReadByte()
        };
    }

    public (Vector3 minimum, Vector3 maximum) GetBoundary() =>
        (Center - Scale * Radius, Center + Scale * Radius);

    public void SetBoundary(Vector3 minimum, Vector3 maximum)
    {
        Center = 0.5f * (minimum + maximum);
        Radius = 0.5f * Vector3.Distance(minimum, maximum);
        Scale = 0.5f * (maximum - minimum) / Radius;
    }

    public bool ContainsNode(SurfaceNode target, float epsilon = 0.25f)
    {
        var a = Scale * (Radius + epsilon);
        var b = target.Scale * target.Radius;
        
        return Center.X - a.X <= target.Center.X - b.X &&
            Center.Y - a.Y <= target.Center.Y - b.Y &&
            Center.Z - a.Z <= target.Center.Z - b.Z &&
            Center.X + a.X >= target.Center.X + b.X &&
            Center.Y + a.Y >= target.Center.Y + b.Y &&
            Center.Z + a.Z >= target.Center.Z + b.Z;
    }

    public bool IntersectsNode(SurfaceNode target, float epsilon = 0.25f)
    {
        var a = Scale * (Radius + epsilon);
        var b = target.Scale * target.Radius;

        return Math.Abs(Center.X - target.Center.X) <= a.X + b.X &&
               Math.Abs(Center.Y - target.Center.Y) <= a.Y + b.Y &&
               Math.Abs(Center.Z - target.Center.Z) <= a.Z + b.Z;
    }

    public BitArray ContainsPoints(IList<Vector3> points, float epsilon = 0.25f)
    {
        var a = Scale * (Radius + epsilon);
        var min = Center - a;
        var max = Center + a;
        var result = new BitArray(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            result[i] =
                min.X < p.X &&
                min.Y < p.Y &&
                min.Z < p.Z &&
                max.X > p.X &&
                max.Y > p.Y &&
                max.Z > p.Z;
        }
        return result;
    }

    public static SurfaceNode GroupNodes(SurfaceNode a, SurfaceNode b)
    {
        Vector3 minA, maxA, minB, maxB;

        (minA, maxA) = a.GetBoundary();
        (minB, maxB) = b.GetBoundary();

        var result = new SurfaceNode() {Left = a, Right = b};
        result.SetBoundary(Vector3.Min(minA, minB), Vector3.Max(maxA, maxB));
        return result;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Center.X);
        writer.Write(Center.Y);
        writer.Write(Center.Z);
        writer.Write(Radius);
        writer.Write((byte)(Scale.X * 0xFA));
        writer.Write((byte)(Scale.Y * 0xFA));
        writer.Write((byte)(Scale.Z * 0xFA));
        writer.Write(Unknown);
    }
}