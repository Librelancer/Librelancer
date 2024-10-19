using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using static LibreLancer.ContentEdit.Model.Quickhull.VectorFunctions;
using static LibreLancer.ContentEdit.Model.Quickhull.QHDebug;

namespace LibreLancer.ContentEdit.Model.Quickhull;

enum MergeType
{
    NonConvexWrtLargerFace = 0,
    NonConvex
}

class QuickhullCS
{
    public double Tolerance = -1;
    public List<Face> Faces = new List<Face>();
    public List<Face> NewFaces = new List<Face>();
    public VertexList Claimed = new VertexList();
    public VertexList Unclaimed = new VertexList();
    public List<Vertex> Vertices;
    public List<Face> DiscardedFaces = new List<Face>();
    public List<int> VertexPointIndices = new List<int>();


    public QuickhullCS(IList<Vector3> points)
    {
        Vertices = new List<Vertex>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            Vertices.Add(new Vertex(points[i], i));
        }
    }

    public void AddVertexToFace(Vertex vertex, Face face)
    {
        vertex.Face = face;
        if (face.Outside == null)
        {
            this.Claimed.Add(vertex);
        }
        else
        {
            this.Claimed.InsertBefore(face.Outside, vertex);
        }

        face.Outside = vertex;
    }


    /**
     * Removes `vertex` for the `claimed` list of vertices, it also makes sure
     * that the link from `face` to the first vertex it sees in `claimed` is
     * linked correctly after the removal
     *
     * @param {Vertex} vertex
     * @param {Face} face
     */
    public void RemoveVertexFromFace(Vertex vertex, Face face)
    {
        if (vertex == face.Outside)
        {
            // fix face.outside link
            if (vertex.Next != null && vertex.Next.Face == face)
            {
                // face has at least 2 outside vertices, move the `outside` reference
                face.Outside = vertex.Next;
            }
            else
            {
                // vertex was the only outside vertex that face had
                face.Outside = null;
            }
        }

        this.Claimed.Remove(vertex);
    }

        /**
   * Removes all the visible vertices that `face` is able to see which are
   * stored in the `claimed` vertext list
   *
   * @param {Face} face
   */
    public Vertex RemoveAllVerticesFromFace(Face face)
    {
        if (face.Outside != null)
        {
            // pointer to the last vertex of this face
            // [..., outside, ..., end, outside, ...]
            //          |           |      |
            //          a           a      b
            var end = face.Outside;
            while (end.Next != null && end.Next.Face == face)
            {
                end = end.Next;
            }

            this.Claimed.RemoveChain(face.Outside, end);
            //                            b
            //                       [ outside, ...]
            //                            |  removes this link
            //     [ outside, ..., end ] -┘
            //          |           |
            //          a           a
            end.Next = null;
            return face.Outside;
        }

        return null;
    }

    /**
   * Removes all the visible vertices that `face` is able to see, additionally
   * checking the following:
   *
   * If `absorbingFace` doesn't exist then all the removed vertices will be
   * added to the `unclaimed` vertex list
   *
   * If `absorbingFace` exists then this method will assign all the vertices of
   * `face` that can see `absorbingFace`, if a vertex cannot see `absorbingFace`
   * it's added to the `unclaimed` vertex list
   *
   * @param {Face} face
   * @param {Face} [absorbingFace]
   */
    public void DeleteFaceVertices(Face face, Face absorbingFace = null)
    {
        var faceVertices = this.RemoveAllVerticesFromFace(face);
        if (faceVertices != null)
        {
            if (absorbingFace == null)
            {
                // mark the vertices to be reassigned to some other face
                this.Unclaimed.AddAll(faceVertices);
            }
            else
            {
                // if there's an absorbing face try to assign as many vertices
                // as possible to it

                // the reference `vertex.next` might be destroyed on
                // `this.addVertexToFace` (see VertexList#add), nextVertex is a
                // reference to it
                Vertex nextVertex = null;
                for (var vertex = faceVertices; vertex != null; vertex = nextVertex)
                {
                    nextVertex = vertex.Next;
                    var distance = absorbingFace.DistanceToPlane(vertex.Point);

                    // check if `vertex` is able to see `absorbingFace`
                    if (distance > this.Tolerance)
                    {
                        this.AddVertexToFace(vertex, absorbingFace);
                    }
                    else
                    {
                        this.Unclaimed.Add(vertex);
                    }
                }
            }
        }
    }

    /**
   * Reassigns as many vertices as possible from the unclaimed list to the new
   * faces
   *
   * @param {Faces[]} newFaces
   */
    public void ResolveUnclaimedPoints(List<Face> newFaces) {
        // cache next vertex so that if `vertex.next` is destroyed it's still
        // recoverable
        var vertexNext = this.Unclaimed.First;
        for (var vertex = vertexNext; vertex != null; vertex = vertexNext)
        {
            vertexNext = vertex.Next;
            var maxDistance = this.Tolerance;
            Face maxFace = null;
            int i = 0;
            for (i = 0; i < newFaces.Count; i += 1)
            {
                var face = newFaces[i];
                if (face.Mark == Mark.Visible)
                {
                    var dist = face.DistanceToPlane(vertex.Point);
                    debug($"dist {dist} for {i}, {face.Offset}, {face.Normal}");
                    if (dist > maxDistance)
                    {
                        maxDistance = dist;
                        maxFace = face;
                    }
                    if (maxDistance > 1000 * this.Tolerance)
                    {
                        break;
                    }
                }
            }
            if (maxFace != null) {
                this.AddVertexToFace(vertex, maxFace);
            }
        }
    }


    public (Vertex v0, Vertex v1, Vertex v2, Vertex v3) ComputeTetrahedronExtremes()
    {
        // initially assume that the first vertex is the min/max
        Vertex minVertexX = Vertices[0], minVertexY = Vertices[0], minVertexZ = Vertices[0];
        Vertex maxVertexX = Vertices[0], maxVertexY = Vertices[0], maxVertexZ = Vertices[0];
        // copy the coordinates of the first vertex to min/max
        Vector3d min = (Vector3d)Vertices[0].Point, max = (Vector3d)Vertices[0].Point;
        // compute the min/max vertex on all 6 directions
        for (int i = 1; i < Vertices.Count; i++)
        {
            var vertex = Vertices[i];
            var point = vertex.Point;
            // update the min coordinates
            if (point.X < min.X)
            {
                minVertexX = vertex;
                min.X = point.X;
            }

            if (point.Y < min.Y)
            {
                minVertexY = vertex;
                min.Y = point.Y;
            }

            if (point.Z < min.Z)
            {
                minVertexZ = vertex;
                min.Z = point.Z;
            }

            // update the max coordinates
            if (point.X > max.X)
            {
                maxVertexX = vertex;
                max.X = point.X;
            }

            if (point.Y > max.Y)
            {
                maxVertexY = vertex;
                max.Y = point.Y;
            }

            if (point.Z > max.Z)
            {
                maxVertexZ = vertex;
                max.Z = point.Z;
            }
        }

        const double JS_EPSILON = 2.2204460492503131e-16;

        Tolerance =
            3 *
            JS_EPSILON *
            (Math.Max(Math.Abs(min.X), Math.Abs(max.X)) +
             Math.Max(Math.Abs(min.Y), Math.Abs(max.Y)) +
             Math.Max(Math.Abs(min.Z), Math.Abs(max.Z)));
        debug($"tolerance {Tolerance}");

        // Find the two vertices with the greatest 1d separation
        // (max.x - min.x)
        // (max.y - min.y)
        // (max.z - min.z)

        Vertex v0 = null;
        Vertex v1 = null;
        var maxDistance = 0f;
        {
            var distance = maxVertexX.Point.X - minVertexX.Point.X;
            if (distance > maxDistance)
            {
                maxDistance = distance;
                v0 = minVertexX;
                v1 = maxVertexX;
            }
        }
        {
            var distance = maxVertexY.Point.Y - minVertexY.Point.Y;
            if (distance > maxDistance)
            {
                maxDistance = distance;
                v0 = minVertexY;
                v1 = maxVertexY;
            }
        }
        {
            var distance = maxVertexZ.Point.Z - minVertexZ.Point.Z;
            if (distance > maxDistance)
            {
                v0 = minVertexZ;
                v1 = maxVertexZ;
            }
        }

        Vertex v2 = null;
        // the next vertex is the one farthest to the line formed by `v0` and `v1`
        maxDistance = 0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            var vertex = Vertices[i];
            if (vertex != v0 && vertex != v1)
            {
                var distance = PointLineDistance(vertex.Point, v0.Point, v1.Point);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    v2 = vertex;
                }
            }
        }

        Vertex v3 = null;
        // the next vertes is the one farthest to the plane `v0`, `v1`, `v2`
        // normalize((v2 - v1) x (v0 - v1))
        var normal = PlaneNormal(v0.Point, v1.Point, v2.Point);
        // distance from the origin to the plane
        var distPO = Vector3.Dot(v0.Point, normal);
        maxDistance = -1;
        for (int i = 0; i < Vertices.Count; i++)
        {
            var vertex = Vertices[i];
            if (vertex != v0 && vertex != v1 && vertex != v2)
            {
                var distance = Math.Abs(Vector3.Dot(normal, vertex.Point) - distPO);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    v3 = vertex;
                }
            }
        }

        return (v0, v1, v2, v3);
    }

    /**
   * Computes a chain of half edges in ccw order called the `horizon`, for an
   * edge to be part of the horizon it must join a face that can see
   * `eyePoint` and a face that cannot see `eyePoint`
   *
   * @param {number[]} eyePoint - The coordinates of a point
   * @param {HalfEdge} crossEdge - The edge used to jump to the current `face`
   * @param {Face} face - The current face being tested
   * @param {HalfEdge[]} horizon - The edges that form part of the horizon in
   * ccw order
   */
    public void ComputeHorizon(Vector3 eyePoint, HalfEdge crossEdge, Face face, List<HalfEdge> horizon)
    {
        //debug("computeHorizon call");
        // moves face's vertices to the `unclaimed` vertex list
        this.DeleteFaceVertices(face);

        face.Mark = Mark.Deleted;
        HalfEdge edge;
        if (crossEdge == null)
        {
            edge = crossEdge = face.GetEdge(0);
        }
        else
        {
            // start from the next edge since `crossEdge` was already analyzed
            // (actually `crossEdge.opposite` was the face who called this method
            // recursively)
            edge = crossEdge.Next;
        }

        // All the faces that are able to see `eyeVertex` are defined as follows
        //
        //       v    /
        //           / <== visible face
        //          /
        //         |
        //         | <== not visible face
        //
        //  dot(v, visible face normal) - visible face offset > this.tolerance
        //
        do
        {
            var oppositeEdge = edge.Opposite;
            var oppositeFace = oppositeEdge.Face;
            if (oppositeFace.Mark == Mark.Visible)
            {
                //debug($"{oppositeFace.DistanceToPlane(eyePoint)}, {Tolerance}, {oppositeFace.Normal}, {oppositeFace.Offset}, {eyePoint}");
                if (oppositeFace.DistanceToPlane(eyePoint) > this.Tolerance)
                {
                    this.ComputeHorizon(eyePoint, oppositeEdge, oppositeFace, horizon);
                }
                else
                {
                    horizon.Add(edge);
                }
            }

            edge = edge.Next;
        } while (edge != crossEdge);
    }

    public bool AllPointsBelongToPlane(Vertex v0, Vertex v1, Vertex v2)
    {
        var normal = PlaneNormal(v0.Point, v1.Point, v2.Point);
        var distToPlane = Vector3.Dot(normal, v0.Point);
        foreach (var vertex in Vertices)
        {
            var dist = Vector3.Dot(vertex.Point, normal);
            if (Math.Abs(dist - distToPlane) > this.Tolerance)
            {
                // A vertex is not part of the plane formed by ((v0 - v1) X (v0 - v2))
                return false;
            }
        }

        return true;
    }

    /**
  * Creates a face with the points `eyeVertex.point`, `horizonEdge.tail` and
  * `horizonEdge.tail` in ccw order
  *
  * @param {Vertex} eyeVertex
  * @param {HalfEdge} horizonEdge
  * @return {HalfEdge} The half edge whose vertex is the eyeVertex
  */
    public HalfEdge AddAdjoiningFace(Vertex eyeVertex, HalfEdge horizonEdge)
    {
        // all the half edges are created in ccw order thus the face is always
        // pointing outside the hull
        // edges:
        //
        //                  eyeVertex.point
        //                       / \
        //                      /   \
        //                  1  /     \  0
        //                    /       \
        //                   /         \
        //                  /           \
        //          horizon.tail --- horizon.head
        //                        2
        //
        var face = Face.CreateTriangle(eyeVertex, horizonEdge.Tail(), horizonEdge.Head());
        this.Faces.Add(face);
        // join face.getEdge(-1) with the horizon's opposite edge
        // face.getEdge(-1) = face.getEdge(2)
        face.GetEdge(-1).SetOpposite(horizonEdge.Opposite);
        return face.GetEdge(0);
    }

    /**
     * Adds horizon.length faces to the hull, each face will be 'linked' with the
     * horizon opposite face and the face on the left/right
     *
     * @param {Vertex} eyeVertex
     * @param {HalfEdge[]} horizon - A chain of half edges in ccw order
     */
    public void AddNewFaces(Vertex eyeVertex, List<HalfEdge> horizon)
    {
        NewFaces = new List<Face>();
        HalfEdge firstSideEdge = null, previousSideEdge = null;
        for (int i = 0; i < horizon.Count; i++)
        {
            var horizonEdge = horizon[i];
            // returns the right side edge
            var sideEdge = this.AddAdjoiningFace(eyeVertex, horizonEdge);
            if (firstSideEdge == null)
            {
                firstSideEdge = sideEdge;
            }
            else
            {
                // joins face.getEdge(1) with previousFace.getEdge(0)
                sideEdge.Next.SetOpposite(previousSideEdge);
            }

            NewFaces.Add(sideEdge.Face);
            previousSideEdge = sideEdge;
        }

        firstSideEdge.Next.SetOpposite(previousSideEdge);
    }

    /**
   * Computes the distance from `edge` opposite face's centroid to
   * `edge.face`
   *
   * @param {HalfEdge} edge
   */
    public double OppositeFaceDistance(HalfEdge edge)
    {
        // - A positive number when the centroid of the opposite face is above the
        //   face i.e. when the faces are concave
        // - A negative number when the centroid of the opposite face is below the
        //   face i.e. when the faces are convex
        return edge.Face.DistanceToPlane(edge.Opposite.Face.Centroid);
    }

    /**
   * Merges a face with none/any/all its neighbors according to the strategy
   * used
   *
   * if `mergeType` is MERGE_NON_CONVEX_WRT_LARGER_FACE then the merge will be
   * decided based on the face with the larger area, the centroid of the face
   * with the smaller area will be checked against the one with the larger area
   * to see if it's in the merge range [tolerance, -tolerance] i.e.
   *
   *    dot(centroid smaller face, larger face normal) - larger face offset > -tolerance
   *
   * Note that the first check (with +tolerance) was done on `computeHorizon`
   *
   * If the above is not true then the check is done with respect to the smaller
   * face i.e.
   *
   *    dot(centroid larger face, smaller face normal) - smaller face offset > -tolerance
   *
   * If true then it means that two faces are non convex (concave), even if the
   * dot(...) - offset value is > 0 (that's the point of doing the merge in the
   * first place)
   *
   * If two faces are concave then the check must also be done on the other face
   * but this is done in another merge pass, for this to happen the face is
   * marked in a temporal NON_CONVEX state
   *
   * if `mergeType` is MERGE_NON_CONVEX then two faces will be merged only if
   * they pass the following conditions
   *
   *    dot(centroid smaller face, larger face normal) - larger face offset > -tolerance
   *    dot(centroid larger face, smaller face normal) - smaller face offset > -tolerance
   *
   * @param {Face} face
   * @param {MergeType} mergeType
   */
    public bool DoAdjacentMerge(Face face, MergeType mergeType)
    {
        var edge = face.Edge;
        var convex = true;
        int it = 0;
        do
        {
            if (it >= face.VertexCount)
            {
                throw new Exception("merge recursion limit exceeded");
            }

            var oppositeFace = edge.Opposite.Face;
            var merge = false;

            // Important notes about the algorithm to merge faces
            //
            // - Given a vertex `eyeVertex` that will be added to the hull
            //   all the faces that cannot see `eyeVertex` are defined as follows
            //
            //      dot(v, not visible face normal) - not visible offset < tolerance
            //
            // - Two faces can be merged when the centroid of one of these faces
            // projected to the normal of the other face minus the other face offset
            // is in the range [tolerance, -tolerance]
            // - Since `face` (given in the input for this method) has passed the
            // check above we only have to check the lower bound e.g.
            //
            //      dot(v, not visible face normal) - not visible offset > -tolerance
            //
            if (mergeType == MergeType.NonConvex)
            {
                if (
                    this.OppositeFaceDistance(edge) > -this.Tolerance ||
                    this.OppositeFaceDistance(edge.Opposite) > -this.Tolerance
                )
                {
                    merge = true;
                }
            }
            else
            {
                if (face.Area > oppositeFace.Area)
                {
                    if (this.OppositeFaceDistance(edge) > -this.Tolerance)
                    {
                        merge = true;
                    }
                    else if (this.OppositeFaceDistance(edge.Opposite) > -this.Tolerance)
                    {
                        convex = false;
                    }
                }
                else
                {
                    if (this.OppositeFaceDistance(edge.Opposite) > -this.Tolerance)
                    {
                        merge = true;
                    }
                    else if (this.OppositeFaceDistance(edge) > -this.Tolerance)
                    {
                        convex = false;
                    }
                }
            }

            if (merge)
            {
                debug("face merge");
                // when two faces are merged it might be possible that redundant faces
                // are destroyed, in that case move all the visible vertices from the
                // destroyed faces to the `unclaimed` vertex list
                var discardedFaces = face.MergeAdjacentFaces(edge, new List<Face>());
                for (var i = 0; i < discardedFaces.Count; i += 1)
                {
                    this.DeleteFaceVertices(discardedFaces[i], face);
                }

                return true;
            }

            edge = edge.Next;
            it += 1;
        } while (edge != face.Edge);

        if (!convex)
        {
            face.Mark = Mark.NonConvex;
        }

        return false;
    }

    /**
       * Adds a vertex to the hull with the following algorithm
       *
       * - Compute the `horizon` which is a chain of half edges, for an edge to
       *   belong to this group it must be the edge connecting a face that can
       *   see `eyeVertex` and a face which cannot see `eyeVertex`
       * - All the faces that can see `eyeVertex` have its visible vertices removed
       *   from the claimed VertexList
       * - A new set of faces is created with each edge of the `horizon` and
       *   `eyeVertex`, each face is connected with the opposite horizon face and
       *   the face on the left/right
       * - The new faces are merged if possible with the opposite horizon face first
       *   and then the faces on the right/left
       * - The vertices removed from all the visible faces are assigned to the new
       *   faces if possible
       *
       * @param {Vertex} eyeVertex
       */
    public void AddVertexToHull(Vertex eyeVertex)
    {
        var horizon = new List<HalfEdge>();
        Unclaimed.Clear();
        // remove `eyeVertex` from `eyeVertex.face` so that it can't be added to the
        // `unclaimed` vertex list
        RemoveVertexFromFace(eyeVertex, eyeVertex.Face);
        ComputeHorizon(eyeVertex.Point, null, eyeVertex.Face, horizon);
        if (IsDebug)
        {
            debug($"horizon {string.Join(',', horizon.Select(x => x.Head().Index.ToString()))}");
        }
        AddNewFaces(eyeVertex, horizon);
        debug("first merge");
        // first merge pass
        // Do the merge with respect to the larger face
        for (var i = 0; i < this.NewFaces.Count; i += 1) {
            var face = this.NewFaces[i];
            if (face.Mark == Mark.Visible) {
                while (this.DoAdjacentMerge(face, MergeType.NonConvexWrtLargerFace)) {}
            }
        }

        debug("second merge");

        // second merge pass
        // Do the merge on non convex faces (a face is marked as non convex in the
        // first pass)
        for (int i = 0; i < this.NewFaces.Count; i ++)
        {
            var face = this.NewFaces[i];
            if (face.Mark == Mark.NonConvex)
            {
                face.Mark = Mark.Visible;
                while (this.DoAdjacentMerge(face, MergeType.NonConvexWrtLargerFace)) {}
            }
        }
        debug("reassigning points to newFaces");
        ResolveUnclaimedPoints(NewFaces);
    }


    public void CreateInitialSimplex(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
    {
        var normal = PlaneNormal(v0.Point, v1.Point, v2.Point);
        var distPO = Vector3.Dot(v0.Point, normal);
        // initial simplex
        // Taken from http://everything2.com/title/How+to+paint+a+tetrahedron
        //
        //                              v2
        //                             ,|,
        //                           ,7``\'VA,
        //                         ,7`   |, `'VA,
        //                       ,7`     `\    `'VA,
        //                     ,7`        |,      `'VA,
        //                   ,7`          `\         `'VA,
        //                 ,7`             |,           `'VA,
        //               ,7`               `\       ,..ooOOTK` v3
        //             ,7`                  |,.ooOOT''`    AV
        //           ,7`            ,..ooOOT`\`           /7
        //         ,7`      ,..ooOOT''`      |,          AV
        //        ,T,..ooOOT''`              `\         /7
        //     v0 `'TTs.,                     |,       AV
        //            `'TTs.,                 `\      /7
        //                 `'TTs.,             |,    AV
        //                      `'TTs.,        `\   /7
        //                           `'TTs.,    |, AV
        //                                `'TTs.,\/7
        //                                     `'T`
        //                                       v1
        //
        var faces = new List<Face>();
        if (Vector3.Dot(v3.Point, normal) - distPO < 0)
        {
            // the face is not able to see the point so `planeNormal`
            // is pointing outside the tetrahedron
            faces.Add(Face.CreateTriangle(v0, v1, v2));
            faces.Add(Face.CreateTriangle(v3, v1, v0));
            faces.Add(Face.CreateTriangle(v3, v2, v1));
            faces.Add(Face.CreateTriangle(v3, v0, v2));
            // set the opposite edge
            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                // join face[i] i > 0, with the first face
                faces[i + 1].GetEdge(2).SetOpposite(faces[0].GetEdge(j));
                // join face[i] with face[i + 1], 1 <= i <= 3
                faces[i + 1].GetEdge(1).SetOpposite(faces[j + 1].GetEdge(0));
            }
        }
        else
        {
            // the face is able to see the point so `planeNormal`
            // is pointing inside the tetrahedron
            faces.Add(Face.CreateTriangle(v0, v2, v1));
            faces.Add(Face.CreateTriangle(v3, v0, v1));
            faces.Add(Face.CreateTriangle(v3, v1, v2));
            faces.Add(Face.CreateTriangle(v3, v2, v0));
            // set the opposite edge
            for (int i = 0; i < 3; i += 1)
            {
                int j = (i + 1) % 3;
                // join face[i] i > 0, with the first face
                faces[i + 1].GetEdge(2).SetOpposite(faces[0].GetEdge((3 - i) % 3));
                // join face[i] with face[i + 1]
                faces[i + 1].GetEdge(0).SetOpposite(faces[j + 1].GetEdge(1));
            }
        }

        // the initial hull is the tetrahedron
        for (int i = 0; i < 4; i++)
        {
            this.Faces.Add(faces[i]);
        }

        // initial assignment of vertices to the faces of the tetrahedron
        for (int i = 0; i < Vertices.Count; i++)
        {
            var vertex = Vertices[i];
            if (vertex != v0 && vertex != v1 && vertex != v2 && vertex != v3)
            {
                var maxDistance = this.Tolerance;
                Face maxFace = null;
                for (int j = 0; j < 4; j += 1)
                {
                    var distance = faces[j].DistanceToPlane(vertex.Point);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        maxFace = faces[j];
                    }
                }

                if (maxFace != null)
                {
                    this.AddVertexToFace(vertex, maxFace);
                }
            }
        }
    }

    public Vertex NextVertexToAdd()
    {
        if (!this.Claimed.IsEmpty)
        {
            Vertex eyeVertex = null, vertex = null;
            double maxDistance = 0;
            var eyeFace = this.Claimed.First.Face;
            for (vertex = eyeFace.Outside; vertex != null && vertex.Face == eyeFace; vertex = vertex.Next)
            {
                var distance = eyeFace.DistanceToPlane(vertex.Point);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    eyeVertex = vertex;
                }
            }

            return eyeVertex;
        }
        debug("claimed list empty");
        return null;
    }


    public void ReindexFaceAndVertices() {
        // remove inactive faces
        var activeFaces = new List<Face>();
        for (var i = 0; i < this.Faces.Count; i += 1)
        {
            var face = this.Faces[i];
            if (face.Mark == Mark.Visible)
            {
                activeFaces.Add(face);
            }
        }
        this.Faces = activeFaces;
    }

    public int[] CollectFaces()
    {
        var faceIndices = new List<int>();
        for (int i = 0; i < Faces.Count; i++)
        {
            if (this.Faces[i].Mark != Mark.Visible)
            {
                throw new Exception("attempt to include a destroyed face in the hull");
            }

            var indices = Faces[i].CollectIndices();
            for (int j = 0; j < indices.Length - 2; j ++)
            {
                faceIndices.Add(indices[0]);
                faceIndices.Add(indices[j + 1]);
                faceIndices.Add(indices[j + 2]);
            }
        }
        return faceIndices.ToArray();
    }


    public void Build()
    {
        int iterations = 0;
        Vertex eyeVertex = null;

        var (v0, v1, v2, v3) = ComputeTetrahedronExtremes();
        if (AllPointsBelongToPlane(v0, v1, v2))
        {
            throw new Exception("Degenerate case");
        }

        CreateInitialSimplex(v0, v1, v2, v3);
        while ((eyeVertex = this.NextVertexToAdd()) != null)
        {
            iterations++;
            debug($"== iteration {iterations} ==");
            debug($"next vertex to add = {eyeVertex.Index}, {eyeVertex.Point}");
            AddVertexToHull(eyeVertex);
            debug($"== end iteration {iterations}");
        }
        debug("reindexing");
        ReindexFaceAndVertices();
    }
}
