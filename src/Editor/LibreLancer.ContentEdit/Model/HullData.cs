using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.ContentEdit.Model.Quickhull;
using LibreLancer.Sur;
using static LibreLancer.ContentEdit.Model.Quickhull.VectorFunctions;

namespace LibreLancer.ContentEdit.Model;

public class HullData
{
    public Vector3[] Vertices;
    public ushort[] Indices;
    public string Source;

    public int FaceCount => Indices.Length / 3;

    public Point3<int> GetFace(int faceIndex)
    {
        faceIndex *= 3;
        return new Point3<int>(Indices[faceIndex], Indices[faceIndex + 1], Indices[faceIndex + 2]);
    }

    public Vector3 GetFaceNormal(int faceIndex)
    {
        faceIndex *= 3;

        var a = Vertices[Indices[faceIndex]];
        var b = Vertices[Indices[faceIndex + 1]];
        var c = Vertices[Indices[faceIndex + 2]];

        var x = c - b;
        var y = a - b;


        var normal = Vector3.Cross(x, y).Normalized();
        return normal;
    }

    public Vector3 GetFaceCenter(int faceIndex)
    {
        faceIndex *= 3;

        var p1 = Vertices[Indices[faceIndex]];
        var p2 = Vertices[Indices[faceIndex + 1]];
        var p3 = Vertices[Indices[faceIndex + 2]];

        var center = (p1 + p2 + p3) / 3f;
        return center;
    }

    public int Raycast(Ray ray)
    {
        for (int i = 0; i < Indices.Length; i += 3)
        {
            if (RayTriangleIntersection(ref ray, float.MaxValue,
                    Vertices[Indices[i]],
                    Vertices[Indices[i + 1]],
                    Vertices[Indices[i + 2]]
                ))
                return i / 3;
        }

        return -1;
    }

    public float GetVolume()
    {
        float vol = 0;
        for (int i = 0; i < FaceCount; i++)
        {
            var f = GetFace(i);
            vol += SignedTriVolume(Vertices[f.A],
                Vertices[f.B],
                Vertices[f.C]);
        }
        return vol;
    }

    static float SignedTriVolume(Vector3 a, Vector3 b, Vector3 c)
        => Vector3.Dot(a, Vector3.Cross(b, c)) / 6;



    static float RayEpsilon = 1E-7f;
    static float BigEpsilon = 1E-5f;

    static bool RayTriangleIntersection(ref Ray ray, float maximumLength, Vector3 a, Vector3 b,
        Vector3 c)
    {
        var ab = b - a;
        var ac = c - a;
        var normal = Vector3.Cross(ac, ab);
        var dn = -Vector3.Dot(ray.Direction, normal);
        var ao = ray.Position - a;
        var t = Vector3.Dot(ao, normal);
        if (t < 0)
        {
            //Impact occurred before the start of the ray.
            return false;
        }
        var aoxd = Vector3.Cross(ao, ray.Direction);
        var v = -Vector3.Dot(ac, aoxd);
        if (v < 0 || v > dn)
        {
            //Invalid barycentric coordinate for b.
            return false;
        }
        var w = Vector3.Dot(ab, aoxd);
        if (w < 0 || v + w > dn)
        {
            //Invalid barycentric coordinate for b and/or c.
            return false;
        }
        return true;
    }

    public static EditResult<HullData> Calculate(IList<Vector3> points)
    {
        return EditResult<HullData>.TryCatch(() =>
        {
            var qh = new Quickhull.QuickhullCS(points);
            qh.Build();
            var idx = qh.CollectFaces();
            var verts = new List<Vector3>();
            var indices = new List<ushort>();
            if (idx.Length > 65535)
                throw new Exception("Generated hull is too complex");
            foreach (var i in idx)
            {
                var v = qh.Vertices[i].Point;
                var x = verts.IndexOf(v);
                if (x == -1)
                {
                    if (verts.Count - 1 > 65535)
                        throw new Exception("Generated hull is too complex");
                    verts.Add(v);
                    indices.Add((ushort)(verts.Count - 1));
                }
                else
                {
                    indices.Add((ushort)x);
                }
            }
            var hd = new HullData() { Vertices = verts.ToArray(), Indices = indices.ToArray() };
            if (!IsConvexHull(hd.Vertices, hd.Indices))
            {
                throw new Exception("Convex hull creation failed");
            }
            return hd;
        });
    }


    public static bool IsConvexHull(Vector3[] vertices, ushort[] indices)
    {
        if (indices.Length < 6)
        {
            return false;
        }

        const double EPSILON = 0.0001; // fairly small epsilon
        for (int i = 0; i < indices.Length; i += 3)
        {
            var v0 = (Vector3d)vertices[indices[i]];
            var v1 = (Vector3d)vertices[indices[i + 1]];
            var v2 = (Vector3d)vertices[indices[i + 2]];

            var normal = PlaneNormal(v0, v1, v2);
            var offset = Vector3d.Dot(normal, v0);
            for (int j = 0; j < vertices.Length; j++)
            {
                if (j == indices[i] ||
                    j == indices[i + 1] ||
                    j == indices[i + 2])
                    continue;
                var d = Vector3d.Dot(normal, (Vector3d)vertices[j]);
                if (d > offset + EPSILON)
                {
                    return false;
                }
            }
        }
        return true;
    }
}
