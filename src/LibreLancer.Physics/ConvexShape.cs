using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

namespace LibreLancer.Physics;

public class ConvexShape
{
    private const int HullShape = 0x4B332211; //11 22 33 4B
    private const int SoupShape = 0x4E332211; //11 22 33 4E

    public byte[] Data { get; private set; }

    public ConvexShape(ConvexMesh mesh)
    {
        BufferPool pool = new(16384);
        pool.Take(mesh.Indices.Length, out Buffer<Vector3> points);
        for (var i = 0; i < mesh.Indices.Length; i++)
            points[i] = mesh.Vertices[mesh.Indices[i]];
        if (ConvexHullHelper.CreateShape(points, pool, out var center, out var convexHull))
        {
            if (convexHull.FaceToVertexIndicesStart.Length <= 2)
            {
                convexHull.Dispose(pool);
            }
            else
            {
                Data = EncodeHull(convexHull, center);
                pool.Clear();
                return;
            }
        }

        pool.Take(mesh.Indices.Length / 3, out Buffer<Triangle> triangles);
        int j = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = new(points[j], points[j + 1], points[j + 2]);
            j += 3;
        }
        Data = EncodeTriangleSoup(triangles);
        pool.Clear();
    }

    public ConvexShape(byte[] data)
    {
        Data = data;
        if (data.Length < ShapeHeader.HeaderSize)
            throw new FormatException("Data buffer is not a ConvexShape (too small)");
        var header = Unsafe.As<byte, ShapeHeader>(ref data[0]);
        if (header.Type != HullShape &&
            header.Type != SoupShape)
            throw new FormatException("Data buffer is not a ConvexShape (magic)");
        if(header.Size != data.Length)
            throw new FormatException("Data buffer is not a ConvexShape (size != len)");
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ShapeHeader
    {
        public const int HeaderSize = 16 + 12;

        public int Type;
        public int FaceCount;
        public int IndexCount;
        public int PointCount;
        public Vector3 Center;

        public int Size => HeaderSize +
                           FaceCount * 5 * sizeof(float) +
                           IndexCount * sizeof(int) +
                           PointCount * sizeof(float) * 3;

        public int PointOffset => HeaderSize;
        public int IndexOffset => PointOffset + (PointCount * sizeof(float) * 3);
        public int FaceOffset => IndexOffset + IndexCount * sizeof(int);

        public int BoundingPlaneOffsetOffset => FaceOffset + FaceCount * sizeof(int);
        public int BoundingPlaneNormalOffset => BoundingPlaneOffsetOffset + FaceCount * sizeof(float);
    }

    internal bool IsHull => Unsafe.As<byte, ShapeHeader>(ref Data[0]).Type == HullShape;

    internal (ConvexHull Hull, Vector3 Center) CreateHull(BufferPool pool)
    {
        var h = Unsafe.As<byte, ShapeHeader>(ref Data[0]);
        if (h.Type != HullShape)
            throw new Exception("Shape isn't convex hull");
        var points = MemoryMarshal.Cast<byte, float>(Data.AsSpan(
            h.PointOffset, h.PointCount * sizeof(float) * 3));
        var indices = MemoryMarshal.Cast<byte, int>(Data.AsSpan(
            h.IndexOffset, h.IndexCount * sizeof(int)));
        var faceStarts = MemoryMarshal.Cast<byte, int>(Data.AsSpan(
            h.FaceOffset, h.FaceCount * sizeof(int)));
        var planeOffsets = MemoryMarshal.Cast<byte, float>(Data.AsSpan(
            h.BoundingPlaneOffsetOffset, h.FaceCount * sizeof(float)));
        var planeNormals = MemoryMarshal.Cast<byte, float>(Data.AsSpan(
            h.BoundingPlaneNormalOffset, h.FaceCount * sizeof(float) * 3));

        var hull = new ConvexHull();

        pool.Take(faceStarts.Length, out hull.FaceToVertexIndicesStart);
        faceStarts.CopyTo(hull.FaceToVertexIndicesStart);

        pool.Take(indices.Length, out hull.FaceVertexIndices);
        for (int i = 0; i < indices.Length; i++)
        {
            BundleIndexing.GetBundleIndices(indices[i], out var bundleIndex, out var innerIndex);
            hull.FaceVertexIndices[i] = new HullVertexIndex() { BundleIndex = (ushort)bundleIndex, InnerIndex = (ushort)innerIndex };
        }

        int pointArraySize = (h.PointCount + Vector<float>.Count - 1) / Vector<float>.Count;
        pool.Take(pointArraySize, out hull.Points);
        Span<float> vec = stackalloc float[Vector<float>.Count];
        vec.Clear();

        for (int i = 0; i < h.PointCount; i += Vector<float>.Count)
        {
            ref var slot = ref hull.Points[i / Vector<float>.Count];
            if (h.PointCount - i >= Vector<float>.Count)
            {
                slot.X = new Vector<float>(points.Slice(i, Vector<float>.Count));
                slot.Y = new Vector<float>(points.Slice(i + h.PointCount, Vector<float>.Count));
                slot.Z = new Vector<float>(points.Slice(i + h.PointCount * 2, Vector<float>.Count));
            }
            else
            {
                // ConvexHullHelper repeats the last vector across the bundle trail
                // so we do the same.
                vec.Fill(points[h.PointCount - 1]);
                points.Slice(i, h.PointCount - i).CopyTo(vec);
                slot.X = new Vector<float>(vec);
                vec.Fill(points[h.PointCount * 2 - 1]);
                points.Slice(i + h.PointCount, h.PointCount - i).CopyTo(vec);
                slot.Y = new Vector<float>(vec);
                vec.Fill(points[h.PointCount * 3 - 1]);
                points.Slice(i + h.PointCount * 2, h.PointCount - i).CopyTo(vec);
                slot.Z = new Vector<float>(vec);
            }
        }

        int planeCount = planeOffsets.Length;
        int boundingPlaneArraySize = (planeCount + Vector<float>.Count - 1) / Vector<float>.Count;
        pool.Take(boundingPlaneArraySize, out hull.BoundingPlanes);

        // Trailing bounding plane volumes have float.MinValue offset and 0 normal
        Span<float> minVec = stackalloc float[Vector<float>.Count];
        minVec.Fill(float.MinValue);
        vec.Clear();

        for (int i = 0; i < planeCount; i += Vector<float>.Count)
        {
            ref var slot = ref hull.BoundingPlanes[i / Vector<float>.Count];
            if (planeCount - i >= Vector<float>.Count)
            {
                slot.Offset = new Vector<float>(planeOffsets.Slice(i, Vector<float>.Count));
                slot.Normal.X = new Vector<float>(planeNormals.Slice(i, Vector<float>.Count));
                slot.Normal.Y = new Vector<float>(planeNormals.Slice(i + planeCount, Vector<float>.Count));
                slot.Normal.Z = new Vector<float>(planeNormals.Slice(i + planeCount * 2, Vector<float>.Count));
            }
            else
            {
                planeOffsets.Slice(i, planeCount - i).CopyTo(minVec);
                slot.Offset = new Vector<float>(minVec);
                planeNormals.Slice(i, planeCount - i).CopyTo(vec);
                slot.Normal.X = new Vector<float>(vec);
                planeNormals.Slice(i + planeCount, planeCount - i).CopyTo(vec);
                slot.Normal.Y = new Vector<float>(vec);
                planeNormals.Slice(i + planeCount * 2, planeCount - i).CopyTo(vec);
                slot.Normal.Z = new Vector<float>(vec);
            }
        }

        return (hull, h.Center);
    }

    internal Triangle[] GetTriangles()
    {
        var h = Unsafe.As<byte, ShapeHeader>(ref Data[0]);
        if (h.Type != SoupShape)
            throw new Exception("Shape isn't triangle soup");
        var points = MemoryMarshal.Cast<byte, Vector3>(Data.AsSpan(
            h.PointOffset, h.PointCount * sizeof(float) * 3));
        var tris = new Triangle[points.Length / 3];
        int j = 0;
        for (int i = 0; i < tris.Length; i++)
        {
            tris[i] = new Triangle(points[j], points[j + 1], points[j + 2]);
            j += 3;
        }
        return tris;
    }

    private static byte[] EncodeHull(ConvexHull hull, Vector3 center)
    {
        int pointsLength = -1;
        for (int i = 0; i < hull.FaceVertexIndices.Length; i++)
        {
            var idx = (hull.FaceVertexIndices[i].BundleIndex * Vector<float>.Count)
                      + hull.FaceVertexIndices[i].InnerIndex;
            if (idx > pointsLength)
                pointsLength = idx;
        }
        pointsLength++;

        var h = new ShapeHeader();
        h.Type = HullShape;
        h.FaceCount = hull.FaceToVertexIndicesStart.Length;
        h.IndexCount = hull.FaceVertexIndices.Length;
        h.PointCount = pointsLength;
        h.Center = center;

        var buffer = new byte[h.Size];
        Unsafe.As<byte, ShapeHeader>(ref buffer[0]) = h;

        var points = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(
            h.PointOffset, h.PointCount * sizeof(float) * 3));
        var indices = MemoryMarshal.Cast<byte, int>(buffer.AsSpan(
            h.IndexOffset, h.IndexCount * sizeof(int)));
        var faceStarts = MemoryMarshal.Cast<byte, int>(buffer.AsSpan(
            h.FaceOffset, h.FaceCount * sizeof(int)));
        var planeOffsets = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(
            h.BoundingPlaneOffsetOffset, h.FaceCount * sizeof(float)));
        var planeNormals = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(
            h.BoundingPlaneNormalOffset, h.FaceCount * sizeof(float) * 3));

        for (int i = 0; i < hull.FaceVertexIndices.Length; i++)
        {
            var idx = (hull.FaceVertexIndices[i].BundleIndex * Vector<float>.Count)
                      + hull.FaceVertexIndices[i].InnerIndex;
            indices[i] = idx;
        }
        hull.FaceToVertexIndicesStart.CopyTo(0, faceStarts, 0, faceStarts.Length);

        for (int i = 0; i < hull.Points.Length; i++)
        {
            int offset = i * Vector<float>.Count;
            if (offset + Vector<float>.Count <= pointsLength)
            {
                hull.Points[i].X.CopyTo(points.Slice(offset));
                hull.Points[i].Y.CopyTo(points.Slice(pointsLength + offset));
                hull.Points[i].Z.CopyTo(points.Slice(pointsLength * 2 + offset));
            }
            else
            {
                int count = pointsLength - offset;
                for (int j = 0; j < count; j++)
                {
                    points[offset + j] = hull.Points[i].X[j];
                    points[pointsLength + offset + j] = hull.Points[i].Y[j];
                    points[pointsLength * 2 + offset + j] = hull.Points[i].Z[j];
                }
            }
        }

        for (int i = 0; i < hull.BoundingPlanes.Length; i++)
        {
            int offset = i * Vector<float>.Count;
            if (offset + Vector<float>.Count <= planeOffsets.Length)
            {
                hull.BoundingPlanes[i].Offset.CopyTo(planeOffsets.Slice(offset));
                hull.BoundingPlanes[i].Normal.X.CopyTo(planeNormals.Slice(offset));
                hull.BoundingPlanes[i].Normal.Y.CopyTo(planeNormals.Slice(h.FaceCount + offset));
                hull.BoundingPlanes[i].Normal.Z.CopyTo(planeNormals.Slice(h.FaceCount * 2 + offset));
            }
            else
            {
                int count = h.FaceCount - offset;
                for (int j = 0; j < count; j++)
                {
                    planeOffsets[offset + j] = hull.BoundingPlanes[i].Offset[j];
                    planeNormals[offset + j] = hull.BoundingPlanes[i].Normal.X[j];
                    planeNormals[h.FaceCount + offset + j] = hull.BoundingPlanes[i].Normal.Y[j];
                    planeNormals[h.FaceCount * 2 + offset + j] = hull.BoundingPlanes[i].Normal.Z[j];
                }
            }
        }
        return buffer;
    }

    static byte[] EncodeTriangleSoup(Buffer<Triangle> triangles)
    {
        var h = new ShapeHeader();
        h.Type = SoupShape;
        h.FaceCount = 0;
        h.IndexCount = 0;
        h.PointCount = triangles.Length * 3;
        h.Center = Vector3.Zero;
        var buffer = new byte[h.Size];
        Unsafe.As<byte, ShapeHeader>(ref buffer[0]) = h;

        var points = MemoryMarshal.Cast<byte, Vector3>(buffer.AsSpan(
            h.PointOffset, h.PointCount * sizeof(float) * 3));
        int j = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            points[j++] = triangles[i].A;
            points[j++] = triangles[i].B;
            points[j++] = triangles[i].C;
        }
        return buffer;
    }
}
