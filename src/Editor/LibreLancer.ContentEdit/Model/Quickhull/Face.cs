using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static LibreLancer.ContentEdit.Model.Quickhull.QHDebug;

namespace LibreLancer.ContentEdit.Model.Quickhull;

enum Mark
{
    Visible,
    NonConvex,
    Deleted
}


class Face
{
    public Vector3d Normal;
    public Vector3d Centroid;
    public double Offset;
    public Vertex Outside;
    public Mark Mark;
    public HalfEdge Edge;
    public int VertexCount;
    public double Area;

    public HalfEdge GetEdge(int i)
    {
        var it = Edge;
        while (i > 0)
        {
            it = it.Next;
            i--;
        }

        while (i < 0)
        {
            it = it.Prev;
            i++;
        }

        return it;
    }

    public void ComputeNormal()
    {
        var e0 = Edge;
        var e1 = e0.Next;
        var e2 = e1.Next;
        var v2 = (Vector3d)e1.Head().Point - (Vector3d)e0.Head().Point;
        Vector3d v1;
        VertexCount = 2;
        Normal = Vector3d.Zero;
        while (e2 != e0)
        {
            v1 = v2;
            v2 = (Vector3d)e2.Head().Point - (Vector3d)e0.Head().Point;
            Normal += Vector3d.Cross(v1, v2);
            e2 = e2.Next;
            VertexCount++;
        }

        Area = Normal.Length();
        Normal = Normal * (1 / Area);
    }

    public void ComputeNormalMinArea(float minArea)
    {
        ComputeNormal();
        if (Area < minArea)
        {
            // compute the normal without the longest edge
            HalfEdge maxEdge = null;
            float maxSquaredLength = 0;
            var edge = Edge;
            do
            {
                var lengthSquared = edge.LengthSquared();
                if (lengthSquared > maxSquaredLength)
                {
                    maxEdge = edge;
                    maxSquaredLength = lengthSquared;
                }
                edge = edge.Next;
            } while (edge != Edge);

            var p1 = (Vector3d)maxEdge.Tail().Point;
            var p2 = (Vector3d)maxEdge.Head().Point;
            var maxVector = p2 - p1;
            maxVector.Normalize();
            // compute the projection of maxVector over this face normal
            var maxProjection = Vector3d.Dot(Normal, maxVector);
            // subtract the quantity maxEdge adds on the normal
            Normal -= (maxVector * maxProjection);
            // renormalize
            Normal.Normalize();
        }
    }

    public void ComputeCentroid()
    {
        Centroid = Vector3d.Zero;
        var e = Edge;
        do
        {
            Centroid += (Vector3d)e.Head().Point;
            e = e.Next;
        } while (e != Edge);

        Centroid /= VertexCount;
    }

    public void ComputeNormalAndCentroid(float minArea = -1)
    {
        if (minArea > 0)
        {
            ComputeNormalMinArea(minArea);
        }
        else
        {
            ComputeNormal();
        }
        ComputeCentroid();
        Offset = Vector3d.Dot(this.Normal, this.Centroid);
    }

    public double DistanceToPlane(Vector3d point)
    {
        return Vector3d.Dot(Normal, point) - Offset;
    }

    public double DistanceToPlane(Vector3 point)
    {
        return Vector3d.Dot(Normal, (Vector3d)point) - Offset;
    }

    public Face ConnectHalfEdges(HalfEdge prev, HalfEdge next)
    {
        Face discardedFace = null;
        if (prev.Opposite.Face == next.Opposite.Face)
        {
            // `prev` is remove a redundant edge
            var oppositeFace = next.Opposite.Face;
            HalfEdge oppositeEdge;
            if (prev == Edge) {
                Edge = next;
            }
            if (oppositeFace.VertexCount == 3) {
                // case:
                // remove the face on the right
                //
                //       /|\
                //      / | \ the face on the right
                //     /  |  \ --> opposite edge
                //    / a |   \
                //   *----*----*
                //  /     b  |  \
                //           ▾
                //      redundant edge
                //
                // Note: the opposite edge is actually in the face to the right
                // of the face to be destroyed
                oppositeEdge = next.Opposite.Prev.Opposite;
                oppositeFace.Mark = Mark.Deleted;
                discardedFace = oppositeFace;
            } else {
                // case:
                //          t
                //        *----
                //       /| <- right face's redundant edge
                //      / | opposite edge
                //     /  |  ▴   /
                //    / a |  |  /
                //   *----*----*
                //  /     b  |  \
                //           ▾
                //      redundant edge
                oppositeEdge = next.Opposite.Next;
                // make sure that the link `oppositeFace.edge` points correctly even
                // after the right face redundant edge is removed
                if (oppositeFace.Edge == oppositeEdge.Prev)
                {
                    oppositeFace.Edge = oppositeEdge;
                }

                //       /|   /
                //      / | t/opposite edge
                //     /  | / ▴  /
                //    / a |/  | /
                //   *----*----*
                //  /     b     \
                oppositeEdge.Prev = oppositeEdge.Prev.Prev;
                oppositeEdge.Prev.Next = oppositeEdge;
            }
            //       /|
            //      / |
            //     /  |
            //    / a |
            //   *----*----*
            //  /     b  ▴  \
            //           |
            //     redundant edge
            next.Prev = prev.Prev;
            next.Prev.Next = next;

            //       / \  \
            //      /   \->\
            //     /     \<-\ opposite edge
            //    / a     \  \
            //   *----*----*
            //  /     b  ^  \
            next.SetOpposite(oppositeEdge);

            oppositeFace.ComputeNormalAndCentroid();
        }
        else
        {
            // trivial case
            //        *
            //       /|\
            //      / | \
            //     /  |--> next
            //    / a |   \
            //   *----*----*
            //    \ b |   /
            //     \  |--> prev
            //      \ | /
            //       \|/
            //        *
            prev.Next = next;
            next.Prev = prev;
        }
        return discardedFace;
    }

    public List<Face> MergeAdjacentFaces(HalfEdge adjacentEdge, List<Face> discardedFaces)
    {
        var oppositeEdge = adjacentEdge.Opposite;
        var oppositeFace = oppositeEdge.Face;

        discardedFaces.Add(oppositeFace);
        oppositeFace.Mark = Mark.Deleted;

        // find the chain of edges whose opposite face is `oppositeFace`
        //
        //                ===>
        //      \         face         /
        //       * ---- * ---- * ---- *
        //      /     opposite face    \
        //                <===
        //
        var adjacentEdgePrev = adjacentEdge.Prev;
        var adjacentEdgeNext = adjacentEdge.Next;
        var oppositeEdgePrev = oppositeEdge.Prev;
        var oppositeEdgeNext = oppositeEdge.Next;

        // left edge
        while (adjacentEdgePrev.Opposite.Face == oppositeFace)
        {
            adjacentEdgePrev = adjacentEdgePrev.Prev;
            oppositeEdgeNext = oppositeEdgeNext.Next;
        }
        // right edge
        while (adjacentEdgeNext.Opposite.Face == oppositeFace)
        {
            adjacentEdgeNext = adjacentEdgeNext.Next;
            oppositeEdgePrev = oppositeEdgePrev.Prev;
        }
        // adjacentEdgePrev  \         face         / adjacentEdgeNext
        //                    * ---- * ---- * ---- *
        // oppositeEdgeNext  /     opposite face    \ oppositeEdgePrev

        // fix the face reference of all the opposite edges that are not part of
        // the edges whose opposite face is not `face` i.e. all the edges that
        // `face` and `oppositeFace` do not have in common
        HalfEdge edge;
        for (edge = oppositeEdgeNext; edge != oppositeEdgePrev.Next; edge = edge.Next)
        {
            edge.Face = this;
        }

        // make sure that `face.edge` is not one of the edges to be destroyed
        // Note: it's important for it to be a `next` edge since `prev` edges
        // might be destroyed on `connectHalfEdges`
        this.Edge = adjacentEdgeNext;

        // connect the extremes
        // Note: it might be possible that after connecting the edges a triangular
        // face might be redundant

        var discardedFace = ConnectHalfEdges(oppositeEdgePrev, adjacentEdgeNext);
        if (discardedFace != null) {
            discardedFaces.Add(discardedFace);
        }

        discardedFace = ConnectHalfEdges(adjacentEdgePrev, oppositeEdgeNext);
        if (discardedFace != null)
        {
            discardedFaces.Add(discardedFace);
        }

        ComputeNormalAndCentroid();

        return discardedFaces;
    }

    public int[] CollectIndices()
    {
        var indices = new List<int>();
        var edge = this.Edge;
        do
        {
            indices.Add(edge.Head().Index);
            edge = edge.Next;
        } while (edge != this.Edge);
        return indices.ToArray();
    }

    public string ToPrintString() => string.Join(' ', CollectIndices().Select(x => x.ToString()));

    public static Face FromVertices(IList<Vertex> vertices, float minArea = 0)
    {
        var face = new Face();
        var e0 = new HalfEdge(vertices[0], face);
        var lastE = e0;
        for (var i = 1; i < vertices.Count; i += 1) {
            var e = new HalfEdge(vertices[i], face);
            e.Prev = lastE;
            lastE.Next = e;
            lastE = e;
        }

        lastE.Next = e0;
        e0.Prev = lastE;
        // main half edge reference
        face.Edge = e0;
        face.ComputeNormalAndCentroid(minArea);
        /*if (IsDebug)
        {
            debug($"face created {face.ToPrintString()}");
        }*/
        return face;
    }

    public static Face CreateTriangle(Vertex v0, Vertex v1, Vertex v2, float minArea = 0)
    {
        var face = new Face();
        var e0 = new HalfEdge(v0, face);
        var e1 = new HalfEdge(v1, face);
        var e2 = new HalfEdge(v2, face);
        // join edges
        e0.Next = e2.Prev = e1;
        e1.Next = e0.Prev = e2;
        e2.Next = e1.Prev = e0;
        // main half edge reference
        face.Edge = e0;
        face.ComputeNormalAndCentroid(minArea);
        /*if (IsDebug)
        {
            debug($"face created {face.ToPrintString()}, a={face.Area}, c={face.Centroid}");
        }*/
        return face;
    }
}
