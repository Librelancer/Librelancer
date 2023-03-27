/*
 * Copyright 2019 Oskar Sigvardsson
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

/* Librelancer Changes:
    Ported to System.Numerics and Debug.Assert instead of UnityEngine
    Removed DEBUG_QUICKHULL and changed to assert+verify in all debug builds
    Return convex hull with arrays
*/

using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace LibreLancer.ContentEdit.Model {

    
	/// <summary>
	///   An implementation of the quickhull algorithm for generating 3d convex
	///   hulls.
	///
	///   The algorithm works like this: you start with an initial "seed" hull,
	///   that is just a simple tetrahedron made up of four points in the point
	///   cloud. This seed hull is then grown until it all the points in the
	///   point cloud is inside of it, at which point it will be the convex hull
	///   for the entire set.
	///
	///   All of the points in the point cloud is divided into two parts, the
	///   "open set" and the "closed set". The open set consists of all the
	///   points outside of the tetrahedron, and the closed set is all of the
	///   points inside the tetrahedron. After each iteration of the algorithm,
	///   the closed set gets bigger and the open set get smaller. When the open
	///   set is empty, the algorithm is finished.
	///
	///   Each point in the open set is assigned to a face that it lies outside
	///   of. To grow the hull, the point in the open set which is farthest from
	///   it's face is chosen. All faces which are facing that point (I call
	///   them "lit faces" in the code, because if you imagine the point as a
	///   point light, it's the set of points which would be lit by that point
	///   light) are removed, and a "horizon" of edges is found from where the
	///   faces were removed. From this horizon, new faces are constructed in a
	///   "cone" like fashion connecting the point to the edges.
	///
	///   To keep track of the faces, I use a struct for each face which
	///   contains the three vertices of the face in CCW order, as well as the
	///   three triangles which share an edge. I was considering doing a
	///   half-edge structure to store the mesh, but it's not needed. Using a
	///   struct for each face and neighbors simplify the algorithm and makes it
	///   easy to export it as a mesh.
	///
	///   The most subtle part of the algorithm is finding the horizon. In order
	///   to properly construct the cone so that all neighbors are kept
	///   consistent, you can do a depth-first search from the first lit face.
	///   If the depth-first search always proceeeds in a counter-clockwise
	///   fashion, it guarantees that the horizon will be found in a
	///   counter-clockwise order, which makes it easy to construct the cone of
	///   new faces.
	///
	///   A note: the code uses a right-handed coordinate system, where the
	///   cross-product uses the right-hand rule and the faces are in CCW order.
	///   At the end of the algorithm, the hull is exported in a Unity-friendly
	///   fashion, with a left-handed mesh.
	/// </summary>
    class ConvexHullCalculator {

		/// <summary>
		///   Constant representing a point that has yet to be assigned to a
		///   face. It's only used immediately after constructing the seed hull.
		/// </summary>
		const int UNASSIGNED = -2;

		/// <summary>
		///   Constant representing a point that is inside the convex hull, and
		///   thus is behind all faces. In the openSet array, all points with
		///   INSIDE are at the end of the array, with indexes larger
		///   openSetTail.
		/// </summary>
		const int INSIDE = -1;

		/// <summary>
		///   Epsilon value. If the coordinates of the point space are
		///   exceptionally close to each other, this value might need to be
		///   adjusted.
		/// </summary>
		const float EPSILON = 0.0001f;

		/// <summary>
		///   Struct representing a single face.
		///
		///   Vertex0, Vertex1 and Vertex2 are the vertices in CCW order. They
		///   acutal points are stored in the points array, these are just
		///   indexes into that array.
		///
		///   Opposite0, Opposite1 and Opposite2 are the keys to the faces which
		///   share an edge with this face. Opposite0 is the face opposite
		///   Vertex0 (so it has an edge with Vertex2 and Vertex1), etc.
		///
		///   Normal is (unsurprisingly) the normal of the triangle.
		/// </summary>
		struct Face {
			public int Vertex0;
			public int Vertex1;
			public int Vertex2;

			public int Opposite0;
			public int Opposite1;
			public int Opposite2;

			public Vector3 Normal;

			public Face(int v0, int v1, int v2, int o0, int o1, int o2, Vector3 normal) {
				Vertex0 = v0;
				Vertex1 = v1;
				Vertex2 = v2;
				Opposite0 = o0;
				Opposite1 = o1;
				Opposite2 = o2;
				Normal = normal;
			}

			public bool Equals(Face other) {
				return (this.Vertex0   == other.Vertex0)
					&& (this.Vertex1   == other.Vertex1)
					&& (this.Vertex2   == other.Vertex2)
					&& (this.Opposite0 == other.Opposite0)
					&& (this.Opposite1 == other.Opposite1)
					&& (this.Opposite2 == other.Opposite2)
					&& (this.Normal    == other.Normal);
			}
		}

		/// <summary>
		///   Struct representing a mapping between a point and a face. These
		///   are used in the openSet array.
		///
		///   Point is the index of the point in the points array, Face is the
		///   key of the face in the Key dictionary, Distance is the distance
		///   from the face to the point.
		/// </summary>
		struct PointFace {
			public int Point;
			public int Face;
			public float Distance;

			public PointFace(int p, int f, float d) {
				Point = p;
				Face = f;
				Distance = d;
			}
		}

		/// <summary>
		///   Struct representing a single edge in the horizon.
		///
		///   Edge0 and Edge1 are the vertexes of edge in CCW order, Face is the
		///   face on the other side of the horizon.
		///
		///   TODO Edge1 isn't actually needed, you can just index the next item
		///   in the horizon array.
		/// </summary>
		struct HorizonEdge {
			public int Face;
			public int Edge0;
			public int Edge1;
		}

		/// <summary>
		///   A dictionary storing the faces of the currently generated convex
		///   hull. The key is the id of the face, used in the Face, PointFace
		///   and HorizonEdge struct.
		///
		///   This is a Dictionary, because we need both random access to it,
		///   the ability to loop through it, and ability to quickly delete
		///   faces (in the ConstructCone method), and Dictionary is the obvious
		///   candidate that can do all of those things.
		///
		///   I'm wondering if using a Dictionary is best idea, though. It might
		///   be better to just have them in a List<Face> and mark a face as
		///   deleted by adding a field to the Face struct. The downside is that
		///   we would need an extra field in the Face struct, and when we're
		///   looping through the points in openSet, we would have to loop
		///   through all the Faces EVER created in the algorithm, and skip the
		///   ones that have been marked as deleted. However, looping through a
		///   list is fairly fast, and it might be worth it to avoid Dictionary
		///   overhead.
		///
		///   TODO test converting to a List<Face> instead.
		/// </summary>
		Dictionary<int, Face> faces;

		/// <summary>
		///   The set of points to be processed. "openSet" is a misleading name,
		///   because it's both the open set (points which are still outside the
		///   convex hull) and the closed set (points that are inside the convex
		///   hull). The first part of the array (with indexes <= openSetTail)
		///   is the openSet, the last part of the array (with indexes >
		///   openSetTail) are the closed set, with Face set to INSIDE. The
		///   closed set is largely irrelevant to the algorithm, the open set is
		///   what matters.
		///
		///   Storing the entire open set in one big list has a downside: when
		///   we're reassigning points after ConstructCone, we only need to
		///   reassign points that belong to the faces that have been removed,
		///   but storing it in one array, we have to loop through the entire
		///   list, and checking litFaces to determine which we can skip and
		///   which need to be reassigned.
		///
		///   The alternative here is to give each face in Face array it's own
		///   openSet. I don't like that solution, because then you have to
		///   juggle so many more heap-allocated List<T>'s, we'd have to use
		///   object pools and such. It would do a lot more allocation, and it
		///   would have worse locality. I should maybe test that solution, but
		///   it probably wont be faster enough (if at all) to justify the extra
		///   allocations.
		/// </summary>
		List<PointFace> openSet;

		/// <summary>
		///   Set of faces which are "lit" by the current point in the set. This
		///   is used in the FindHorizon() DFS search to keep track of which
		///   faces we've already visited, and in the ReassignPoints() method to
		///   know which points need to be reassigned.
		/// </summary>
		HashSet<int> litFaces;

		/// <summary>
		///   The current horizon. Generated by the FindHorizon() DFS search,
		///   and used in ConstructCone to construct new faces. The list of
		///   edges are in CCW order.
		/// </summary>
		List<HorizonEdge> horizon;

		/// <summary>
		///   If SplitVerts is false, this Dictionary is used to keep track of
		///   which points we've added to the final mesh.
		/// </summary>
		Dictionary<int, int> hullVerts;

		/// <summary>
		///   The "tail" of the openSet, the last index of a vertex that has
		///   been assigned to a face.
		/// </summary>
		int openSetTail = -1;

		/// <summary>
		///   When adding a new face to the faces Dictionary, use this for the
		///   key and then increment it.
		/// </summary>
		int faceCount = 0;

		/// <summary>
		///   Generate a convex hull from points in points array, and store the
		///   mesh in Unity-friendly format in verts and tris. If splitVerts is
		///   true, the the verts will be split, if false, the same vert will be
		///   used for more than one triangle.
		/// </summary>
		public HullData GenerateHull(
			List<Vector3> points,
			bool splitVerts)
        {
			if (points.Count < 4) {
				throw new System.ArgumentException("Need at least 4 points to generate a convex hull");
			}

			Initialize(points, splitVerts);

			GenerateInitialHull(points);

			while (openSetTail >= 0) {
				GrowHull(points);
			}

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var normals = new List<Vector3>();
			ExportMesh(points, splitVerts, ref verts, ref tris, ref normals);

            if (verts.Count > 65535)
                throw new System.ArgumentException("Input mesh is too complex");
            
			VerifyMesh(points, ref verts, ref tris);

            return new HullData()
            {
                Vertices = verts.ToArray(),
                Indices = tris.Select(x => (ushort) x).ToArray()
            };
        }

		/// <summary>
		///   Make sure all the buffers and variables needed for the algorithm
		///   are initialized.
		/// </summary>
		void Initialize(List<Vector3> points, bool splitVerts) {
			faceCount = 0;
			openSetTail = -1;

			if (faces == null) {
				faces = new Dictionary<int, Face>();
				litFaces = new HashSet<int>();
				horizon = new List<HorizonEdge>();
				openSet = new List<PointFace>(points.Count);
			} else {
				faces.Clear();
				litFaces.Clear();
				horizon.Clear();
				openSet.Clear();

				if (openSet.Capacity < points.Count) {
					// i wonder if this is a good idea... if you call
					// GenerateHull over and over with slightly increasing
					// points counts, it's going to reallocate every time. Maybe
					// i should just use .Add(), and let the List<T> manage the
					// capacity, increasing it geometrically every time we need
					// to reallocate.

					// maybe do
					//   openSet.Capacity = Mathf.NextPowerOfTwo(points.Count)
					// instead?

					openSet.Capacity = points.Count;
				}
			}

			if (!splitVerts) {
				if (hullVerts == null) {
					hullVerts = new Dictionary<int, int>();
				} else {
					hullVerts.Clear();
				}
			}
		}

		/// <summary>
		///   Create initial seed hull.
		/// </summary>
		void GenerateInitialHull(List<Vector3> points) {
			// Find points suitable for use as the seed hull. Some varieties of
			// this algorithm pick extreme points here, but I'm not convinced
			// you gain all that much from that. Currently what it does is just
			// find the first four points that are not coplanar.
			int b0, b1, b2, b3;
			FindInitialHullIndices(points, out b0, out b1, out b2, out b3);

			var v0 = points[b0];
			var v1 = points[b1];
			var v2 = points[b2];
			var v3 = points[b3];

			var above = Vector3.Dot(v3 - v1, Vector3.Cross(v1 - v0, v2 - v0)) > 0.0f;

			// Create the faces of the seed hull. You need to draw a diagram
			// here, otherwise it's impossible to know what's going on :)

			// Basically: there are two different possible start-tetrahedrons,
			// depending on whether the fourth point is above or below the base
			// triangle. If you draw a tetrahedron with these coordinates (in a
			// right-handed coordinate-system):

			//   b0 = (0,0,0)
			//   b1 = (1,0,0)
			//   b2 = (0,1,0)
			//   b3 = (0,0,1)

			// you can see the first case (set b3 = (0,0,-1) for the second
			// case). The faces are added with the proper references to the
			// faces opposite each vertex

			faceCount = 0;
			if (above) {
				faces[faceCount++] = new Face(b0, b2, b1, 3, 1, 2, Normal(points[b0], points[b2], points[b1]));
				faces[faceCount++] = new Face(b0, b1, b3, 3, 2, 0, Normal(points[b0], points[b1], points[b3]));
				faces[faceCount++] = new Face(b0, b3, b2, 3, 0, 1, Normal(points[b0], points[b3], points[b2]));
				faces[faceCount++] = new Face(b1, b2, b3, 2, 1, 0, Normal(points[b1], points[b2], points[b3]));
			} else {
				faces[faceCount++] = new Face(b0, b1, b2, 3, 2, 1, Normal(points[b0], points[b1], points[b2]));
				faces[faceCount++] = new Face(b0, b3, b1, 3, 0, 2, Normal(points[b0], points[b3], points[b1]));
				faces[faceCount++] = new Face(b0, b2, b3, 3, 1, 0, Normal(points[b0], points[b2], points[b3]));
				faces[faceCount++] = new Face(b1, b3, b2, 2, 0, 1, Normal(points[b1], points[b3], points[b2]));
			}

			VerifyFaces(points);

			// Create the openSet. Add all points except the points of the seed
			// hull.
			for (int i = 0; i < points.Count; i++) {
				if (i == b0 || i == b1 || i == b2 || i == b3) continue;

				openSet.Add(new PointFace(i, UNASSIGNED, 0.0f));
			}

			// Add the seed hull verts to the tail of the list.
			openSet.Add(new PointFace(b0, INSIDE, float.NaN));
			openSet.Add(new PointFace(b1, INSIDE, float.NaN));
			openSet.Add(new PointFace(b2, INSIDE, float.NaN));
			openSet.Add(new PointFace(b3, INSIDE, float.NaN));

			// Set the openSetTail value. Last item in the array is
			// openSet.Count - 1, but four of the points (the verts of the seed
			// hull) are part of the closed set, so move openSetTail to just
			// before those.
			openSetTail = openSet.Count - 5;

			Debug.Assert(openSet.Count == points.Count);

			// Assign all points of the open set. This does basically the same
			// thing as ReassignPoints()
			for (int i = 0; i <= openSetTail; i++) {
				Debug.Assert(openSet[i].Face == UNASSIGNED);
				Debug.Assert(openSet[openSetTail].Face == UNASSIGNED);
				Debug.Assert(openSet[openSetTail + 1].Face == INSIDE);

				var assigned = false;
				var fp = openSet[i];

				Debug.Assert(faces.Count == 4);
				Debug.Assert(faces.Count == faceCount);
				for (int j = 0; j < 4; j++) {
					Debug.Assert(faces.ContainsKey(j));

					var face = faces[j];

					var dist = PointFaceDistance(points[fp.Point], points[face.Vertex0], face);

					if (dist > 0) {
						fp.Face = j;
						fp.Distance = dist;
						openSet[i] = fp;

						assigned = true;
						break;
					}
				}

				if (!assigned) {
					// Point is inside
					fp.Face = INSIDE;
					fp.Distance = float.NaN;

					// Point is inside seed hull: swap point with tail, and move
					// openSetTail back. We also have to decrement i, because
					// there's a new item at openSet[i], and we need to process
					// it next iteration
					openSet[i] = openSet[openSetTail];
					openSet[openSetTail] = fp;

					openSetTail -= 1;
					i -= 1;
				}
			}

			VerifyOpenSet(points);
		}

		/// <summary>
		///   Find four points in the point cloud that are not coplanar for the
		///   seed hull
		/// </summary>
		void FindInitialHullIndices(List<Vector3> points, out int b0, out int b1, out int b2, out int b3) {
			var count = points.Count;

			for (int i0 = 0; i0 < count - 3; i0++) {
				for (int i1 = i0 + 1; i1 < count - 2; i1++) {
					var p0 = points[i0];
					var p1 = points[i1];

					if (AreCoincident(p0, p1)) continue;

					for (int i2 = i1 + 1; i2 < count - 1; i2++) {
						var p2 = points[i2];

						if (AreCollinear(p0, p1, p2)) continue;

						for (int i3 = i2 + 1; i3 < count - 0; i3++) {
							var p3 = points[i3];

							if(AreCoplanar(p0, p1, p2, p3)) continue;

							b0 = i0;
							b1 = i1;
							b2 = i2;
							b3 = i3;
							return;
						}
					}
				}
			}

			throw new System.ArgumentException("Can't generate hull, points are coplanar");
		}

		/// <summary>
		///   Grow the hull. This method takes the current hull, and expands it
		///   to encompass the point in openSet with the point furthest away
		///   from its face.
		/// </summary>
		void GrowHull(List<Vector3> points) {
			Debug.Assert(openSetTail >= 0);
			Debug.Assert(openSet[0].Face != INSIDE);

			// Find farthest point and first lit face.
			var farthestPoint = 0;
			var dist = openSet[0].Distance;

			for (int i = 1; i <= openSetTail; i++) {
				if (openSet[i].Distance > dist) {
					farthestPoint = i;
					dist = openSet[i].Distance;
				}
			}

			// Use lit face to find horizon and the rest of the lit
			// faces.
			FindHorizon(
				points,
				points[openSet[farthestPoint].Point],
				openSet[farthestPoint].Face,
				faces[openSet[farthestPoint].Face]);

			VerifyHorizon();

			// Construct new cone from horizon
			ConstructCone(points, openSet[farthestPoint].Point);

			VerifyFaces(points);

			// Reassign points
			ReassignPoints(points);
		}

		/// <summary>
		///   Start the search for the horizon.
		///
		///   The search is a DFS search that searches neighboring triangles in
		///   a counter-clockwise fashion. When it find a neighbor which is not
		///   lit, that edge will be a line on the horizon. If the search always
		///   proceeds counter-clockwise, the edges of the horizon will be found
		///   in counter-clockwise order.
		///
		///   The heart of the search can be found in the recursive
		///   SearchHorizon() method, but the the first iteration of the search
		///   is special, because it has to visit three neighbors (all the
		///   neighbors of the initial triangle), while the rest of the search
		///   only has to visit two (because one of them has already been
		///   visited, the one you came from).
		/// </summary>
		void FindHorizon(List<Vector3> points, Vector3 point, int fi, Face face) {
			// TODO should I use epsilon in the PointFaceDistance comparisons?

			litFaces.Clear();
			horizon.Clear();

			litFaces.Add(fi);

			Debug.Assert(PointFaceDistance(point, points[face.Vertex0], face) > 0.0f);

			// For the rest of the recursive search calls, we first check if the
			// triangle has already been visited and is part of litFaces.
			// However, in this first call we can skip that because we know it
			// can't possibly have been visited yet, since the only thing in
			// litFaces is the current triangle.
			{
				var oppositeFace = faces[face.Opposite0];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = face.Opposite0,
							Edge0 = face.Vertex1,
							Edge1 = face.Vertex2,
						});
				} else {
					SearchHorizon(points, point, fi, face.Opposite0, oppositeFace);
				}
			}

			if (!litFaces.Contains(face.Opposite1)) {
				var oppositeFace = faces[face.Opposite1];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = face.Opposite1,
							Edge0 = face.Vertex2,
							Edge1 = face.Vertex0,
						});
				} else {
					SearchHorizon(points, point, fi, face.Opposite1, oppositeFace);
				}
			}

			if (!litFaces.Contains(face.Opposite2)) {
				var oppositeFace = faces[face.Opposite2];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = face.Opposite2,
							Edge0 = face.Vertex0,
							Edge1 = face.Vertex1,
						});
				} else {
					SearchHorizon(points, point, fi, face.Opposite2, oppositeFace);
				}
			}
		}

		/// <summary>
		///   Recursively search to find the horizon or lit set.
		/// </summary>
		void SearchHorizon(List<Vector3> points, Vector3 point, int prevFaceIndex, int faceCount, Face face) {
			Debug.Assert(prevFaceIndex >= 0);
			Debug.Assert(litFaces.Contains(prevFaceIndex));
			Debug.Assert(!litFaces.Contains(faceCount));
			Debug.Assert(faces[faceCount].Equals(face));

			litFaces.Add(faceCount);

			// Use prevFaceIndex to determine what the next face to search will
			// be, and what edges we need to cross to get there. It's important
			// that the search proceeds in counter-clockwise order from the
			// previous face.
			int nextFaceIndex0;
			int nextFaceIndex1;
			int edge0;
			int edge1;
			int edge2;

			if (prevFaceIndex == face.Opposite0) {
				nextFaceIndex0 = face.Opposite1;
				nextFaceIndex1 = face.Opposite2;

				edge0 = face.Vertex2;
				edge1 = face.Vertex0;
				edge2 = face.Vertex1;
			} else if (prevFaceIndex == face.Opposite1) {
				nextFaceIndex0 = face.Opposite2;
				nextFaceIndex1 = face.Opposite0;

				edge0 = face.Vertex0;
				edge1 = face.Vertex1;
				edge2 = face.Vertex2;
			} else {
				Debug.Assert(prevFaceIndex == face.Opposite2);

				nextFaceIndex0 = face.Opposite0;
				nextFaceIndex1 = face.Opposite1;

				edge0 = face.Vertex1;
				edge1 = face.Vertex2;
				edge2 = face.Vertex0;
			}

			if (!litFaces.Contains(nextFaceIndex0)) {
				var oppositeFace = faces[nextFaceIndex0];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = nextFaceIndex0,
							Edge0 = edge0,
							Edge1 = edge1,
						});
				} else {
					SearchHorizon(points, point, faceCount, nextFaceIndex0, oppositeFace);
				}
			}

			if (!litFaces.Contains(nextFaceIndex1)) {
				var oppositeFace = faces[nextFaceIndex1];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= 0.0f) {
					horizon.Add(new HorizonEdge {
							Face = nextFaceIndex1,
							Edge0 = edge1,
							Edge1 = edge2,
						});
				} else {
					SearchHorizon(points, point, faceCount, nextFaceIndex1, oppositeFace);
				}
			}
		}

		/// <summary>
		///   Remove all lit faces and construct new faces from the horizon in a
		///   "cone-like" fashion.
		///
		///   This is a relatively straight-forward procedure, given that the
		///   horizon is handed to it in already sorted counter-clockwise. The
		///   neighbors of the new faces are easy to find: they're the previous
		///   and next faces to be constructed in the cone, as well as the face
		///   on the other side of the horizon. We also have to update the face
		///   on the other side of the horizon to reflect it's new neighbor from
		///   the cone.
		/// </summary>
		void ConstructCone(List<Vector3> points, int farthestPoint) {
			foreach (var fi in litFaces) {
				Debug.Assert(faces.ContainsKey(fi));
				faces.Remove(fi);
			}

			var firstNewFace = faceCount;

			for (int i = 0; i < horizon.Count; i++) {
				// Vertices of the new face, the farthest point as well as the
				// edge on the horizon. Horizon edge is CCW, so the triangle
				// should be as well.
				var v0 = farthestPoint;
				var v1 = horizon[i].Edge0;
				var v2 = horizon[i].Edge1;

				// Opposite faces of the triangle. First, the edge on the other
				// side of the horizon, then the next/prev faces on the new cone
				var o0 = horizon[i].Face;
				var o1 = (i == horizon.Count - 1) ? firstNewFace : firstNewFace + i + 1;
				var o2 = (i == 0) ? (firstNewFace + horizon.Count - 1) : firstNewFace + i - 1;

				var fi = faceCount++;

				faces[fi] = new Face(
					v0, v1, v2,
					o0, o1, o2,
					Normal(points[v0], points[v1], points[v2]));

				var horizonFace = faces[horizon[i].Face];

				if (horizonFace.Vertex0 == v1) {
					Debug.Assert(v2 == horizonFace.Vertex2);
					horizonFace.Opposite1 = fi;
				} else if (horizonFace.Vertex1 == v1) {
					Debug.Assert(v2 == horizonFace.Vertex0);
					horizonFace.Opposite2 = fi;
				} else {
					Debug.Assert(v1 == horizonFace.Vertex2);
					Debug.Assert(v2 == horizonFace.Vertex1);
					horizonFace.Opposite0 = fi;
				}

				faces[horizon[i].Face] = horizonFace;
			}
		}

		/// <summary>
		///   Reassign points based on the new faces added by ConstructCone().
		///
		///   Only points that were previous assigned to a removed face need to
		///   be updated, so check litFaces while looping through the open set.
		///
		///   There is a potential optimization here: there's no reason to loop
		///   through the entire openSet here. If each face had it's own
		///   openSet, we could just loop through the openSets in the removed
		///   faces. That would make the loop here shorter.
		///
		///   However, to do that, we would have to juggle A LOT more List<T>'s,
		///   and we would need an object pool to manage them all without
		///   generating a whole bunch of garbage. I don't think it's worth
		///   doing that to make this loop shorter, a straight for-loop through
		///   a list is pretty darn fast. Still, it might be worth trying
		/// </summary>
		void ReassignPoints(List<Vector3> points) {
			for (int i = 0; i <= openSetTail; i++) {
				var fp = openSet[i];

				if (litFaces.Contains(fp.Face)) {
					var assigned = false;
					var point = points[fp.Point];

					foreach (var kvp in faces) {
						var fi = kvp.Key;
						var face = kvp.Value;

						var dist = PointFaceDistance(
							point,
							points[face.Vertex0],
							face);

						if (dist > EPSILON) {
							assigned = true;

							fp.Face = fi;
							fp.Distance = dist;

							openSet[i] = fp;
							break;
						}
					}

					if (!assigned) {
						// If point hasn't been assigned, then it's inside the
						// convex hull. Swap it with openSetTail, and decrement
						// openSetTail. We also have to decrement i, because
						// there's now a new thing in openSet[i], so we need i
						// to remain the same the next iteration of the loop.
						fp.Face = INSIDE;
						fp.Distance = float.NaN;

						openSet[i] = openSet[openSetTail];
						openSet[openSetTail] = fp;

						i--;
						openSetTail--;
					}
				}
			}
		}

		/// <summary>
		///   Final step in algorithm, export the faces of the convex hull in a
		///   mesh-friendly format.
		///
		///   TODO normals calculation for non-split vertices. Right now it just
		///   leaves the normal array empty.
		/// </summary>
		void ExportMesh(
			List<Vector3> points,
			bool splitVerts,
			ref List<Vector3> verts,
			ref List<int> tris,
			ref List<Vector3> normals)
		{
			if (verts == null) {
				verts = new List<Vector3>();
			} else {
				verts.Clear();
			}

			if (tris == null) {
				tris = new List<int>();
			} else {
				tris.Clear();
			}

			if (normals == null) {
				normals = new List<Vector3>();
			} else {
				normals.Clear();
			}

			foreach (var face in faces.Values) {
				int vi0, vi1, vi2;

				if (splitVerts) {
					vi0 = verts.Count; verts.Add(points[face.Vertex0]);
					vi1 = verts.Count; verts.Add(points[face.Vertex1]);
					vi2 = verts.Count; verts.Add(points[face.Vertex2]);

					normals.Add(face.Normal);
					normals.Add(face.Normal);
					normals.Add(face.Normal);
				} else {
					if (!hullVerts.TryGetValue(face.Vertex0, out vi0)) {
						vi0 = verts.Count;
						hullVerts[face.Vertex0] = vi0;
						verts.Add(points[face.Vertex0]);
					}

					if (!hullVerts.TryGetValue(face.Vertex1, out vi1)) {
						vi1 = verts.Count;
						hullVerts[face.Vertex1] = vi1;
						verts.Add(points[face.Vertex1]);
					}

					if (!hullVerts.TryGetValue(face.Vertex2, out vi2)) {
						vi2 = verts.Count;
						hullVerts[face.Vertex2] = vi2;
						verts.Add(points[face.Vertex2]);
					}
				}

				tris.Add(vi0);
				tris.Add(vi1);
				tris.Add(vi2);
			}
		}

		/// <summary>
		///   Signed distance from face to point (a positive number means that
		///   the point is above the face)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float PointFaceDistance(Vector3 point, Vector3 pointOnFace, Face face) {
			return Vector3.Dot(face.Normal, point - pointOnFace);
		}

		/// <summary>
		///   Calculate normal for triangle
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Vector3 Normal(Vector3 v0, Vector3 v1, Vector3 v2) {
			return Vector3.Cross(v1 - v0, v2 - v0).Normalized();
		}


        /// <summary>
		///   Check if two points are coincident
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool AreCoincident(Vector3 a, Vector3 b) {
			return (a - b).Length() <= EPSILON;
		}

		/// <summary>
		///   Check if three points are collinear
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool AreCollinear(Vector3 a, Vector3 b, Vector3 c) {
			return Vector3.Cross(c - a, c - b).Length() <= EPSILON;
		}

		/// <summary>
		///   Check if four points are coplanar
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool AreCoplanar(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
			var n1 = Vector3.Cross(c - a, c - b);
			var n2 = Vector3.Cross(d - a, d - b);

			var m1 = n1.Length();
			var m2 = n2.Length();

			return m1 <= EPSILON
				|| m2 <= EPSILON
				|| AreCollinear(Vector3.Zero,
					(1.0f / m1) * n1,
					(1.0f / m2) * n2);
		}

		/// <summary>
		///   Method used for debugging, verifies that the openSet is in a
		///   sensible state. Conditionally compiled if DEBUG_QUICKHULL if
		///   defined.
		/// </summary>
		[Conditional("DEBUG_QUICKHULL")]
		void VerifyOpenSet(List<Vector3> points) {
			for (int i = 0; i < openSet.Count; i++) {
				if (i > openSetTail) {
					Debug.Assert(openSet[i].Face == INSIDE);
				} else {
					Debug.Assert(openSet[i].Face != INSIDE);
					Debug.Assert(openSet[i].Face != UNASSIGNED);

					Debug.Assert(PointFaceDistance(
							points[openSet[i].Point],
							points[faces[openSet[i].Face].Vertex0],
							faces[openSet[i].Face]) > 0.0f);
				}
			}
		}

		/// <summary>
		///   Method used for debugging, verifies that the horizon is in a
		///   sensible state. Conditionally compiled if DEBUG_QUICKHULL if
		///   defined.
		/// </summary>
		[Conditional("DEBUG_QUICKHULL")]
		void VerifyHorizon() {
			for (int i = 0; i < horizon.Count; i++) {
				var prev = i == 0 ? horizon.Count - 1 : i - 1;

				Debug.Assert(horizon[prev].Edge1 == horizon[i].Edge0);
				Debug.Assert(HasEdge(faces[horizon[i].Face], horizon[i].Edge1, horizon[i].Edge0));
			}
		}

		/// <summary>
		///   Method used for debugging, verifies that the faces array is in a
		///   sensible state. Conditionally compiled if DEBUG is
		///   defined.
		/// </summary>
		[Conditional("DEBUG")]
		void VerifyFaces(List<Vector3> points) {
			foreach (var kvp in faces) {
				var fi = kvp.Key;
				var face = kvp.Value;

				Debug.Assert(faces.ContainsKey(face.Opposite0));
				Debug.Assert(faces.ContainsKey(face.Opposite1));
				Debug.Assert(faces.ContainsKey(face.Opposite2));

				Debug.Assert(face.Opposite0 != fi);
				Debug.Assert(face.Opposite1 != fi);
				Debug.Assert(face.Opposite2 != fi);

				Debug.Assert(face.Vertex0 != face.Vertex1);
				Debug.Assert(face.Vertex0 != face.Vertex2);
				Debug.Assert(face.Vertex1 != face.Vertex2);

				Debug.Assert(HasEdge(faces[face.Opposite0], face.Vertex2, face.Vertex1));
				Debug.Assert(HasEdge(faces[face.Opposite1], face.Vertex0, face.Vertex2));
				Debug.Assert(HasEdge(faces[face.Opposite2], face.Vertex1, face.Vertex0));

				Debug.Assert((face.Normal - Normal(
							points[face.Vertex0],
							points[face.Vertex1],
							points[face.Vertex2])).Length() < EPSILON);
			}
		}

		/// <summary>
		///   Method used for debugging, verifies that the final mesh is
		///   actually a convex hull of all the points. Conditionally compiled
		///   if DEBUG is defined.
		/// </summary>
		[Conditional("DEBUG")]
		void VerifyMesh(List<Vector3> points, ref List<Vector3> verts, ref List<int> tris) {
			Debug.Assert(tris.Count % 3 == 0);

			for (int i = 0; i < points.Count; i++) {
				for (int j = 0; j < tris.Count; j+=3) {
					var t0 = verts[tris[j]];
					var t1 = verts[tris[j + 1]];
					var t2 = verts[tris[j + 2]];

					Debug.Assert(Vector3.Dot(points[i] - t0, Vector3.Cross(t1 - t0, t2 - t0)) <= EPSILON);
				}

			}
		}

		/// <summary>
		///   Does face f have a face with vertexes e0 and e1? Used only for
		///   debugging.
		/// </summary>
		bool HasEdge(Face f, int e0, int e1) {
			return (f.Vertex0 == e0 && f.Vertex1 == e1)
				|| (f.Vertex1 == e0 && f.Vertex2 == e1)
				|| (f.Vertex2 == e0 && f.Vertex0 == e1);
		}
    }
}
