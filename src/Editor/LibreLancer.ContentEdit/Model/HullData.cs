using System;
using System.Numerics;
using LibreLancer.Sur;

namespace LibreLancer.ContentEdit.Model;

class HullData
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
}