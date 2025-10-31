using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SimpleMesh.Convex;

namespace LibreLancer.ContentEdit.Model;

public class HullData
{
    public Hull Hull;
    public string Source;

    public int FaceCount => Hull.Indices.Count / 3;

    public int Raycast(Ray ray)
    {
        for (int i = 0; i < Hull.Indices.Count; i += 3)
        {
            if (RayTriangleIntersection(ref ray, float.MaxValue,
                    Hull.Vertices[Hull.Indices[i]],
                    Hull.Vertices[Hull.Indices[i + 1]],
                    Hull.Vertices[Hull.Indices[i + 2]]
                ))
                return i / 3;
        }

        return -1;
    }

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

    public float CalculateVolume()
    {
        float totalVolume = 0.0f;
        for (int i = 0; i < Hull.Indices.Count; i += 3)
        {
            Vector3 p1 = Hull.Vertices[Hull.Indices[i]];
            Vector3 p2 = Hull.Vertices[Hull.Indices[i + 1]];
            Vector3 p3 = Hull.Vertices[Hull.Indices[i + 2]];
            // Compute the signed volume of the tetrahedron formed by the triangle and the origin
            float tetrahedronVolume = Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6.0f;

            totalVolume += tetrahedronVolume;
        }
        return totalVolume;
    }

    public static EditResult<HullData> Calculate(IList<Vector3> points)
    {
        if (!Hull.TryQuickhull(points.ToArray(), out var hull))
        {
            return EditResult<HullData>.Error("Convex hull creation failed");
        }
        if (hull.Vertices.Count > 65535 || hull.Indices.Count > 65535)
        {
            return EditResult<HullData>.Error("Generated hull is too complex");
        }
        return new HullData() { Hull = hull }.AsResult();
    }
}
