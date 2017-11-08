/* Copyright (C) <2009-2011> <Thorben Linneweber, Jitter Physics>
* 
*  This software is provided 'as-is', without any express or implied
*  warranty.  In no event will the authors be held liable for any damages
*  arising from the use of this software.
*
*  Permission is granted to anyone to use this software for any purpose,
*  including commercial applications, and to alter it and redistribute it
*  freely, subject to the following restrictions:
*
*  1. The origin of this software must not be misrepresented; you must not
*      claim that you wrote the original software. If you use this software
*      in a product, an acknowledgment in the product documentation would be
*      appreciated but is not required.
*  2. Altered source versions must be plainly marked as such, and must not be
*      misrepresented as being the original software.
*  3. This notice may not be removed or altered from any source distribution. 
*/

#region Using Statements
using System;
using System.Collections.Generic;

using LibreLancer.Jitter.Dynamics;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Jitter.Collision.Shapes;
#endregion

namespace LibreLancer.Jitter.Collision.Shapes
{

    /// <summary>
    /// Gets called when a shape changes one of the parameters.
    /// For example the size of a box is changed.
    /// </summary>
    public delegate void ShapeUpdatedHandler();

    /// <summary>
    /// Represents the collision part of the RigidBody. A shape is mainly definied through it's supportmap.
    /// Shapes represent convex objects. Inherited classes have to overwrite the supportmap function.
    /// To implement you own shape: derive a class from <see cref="Shape"/>, implement the support map function
    /// and call 'UpdateShape' within the constructor. GeometricCenter, Mass, BoundingBox and Inertia is calculated numerically
    /// based on your SupportMap implementation.
    /// </summary>
    public abstract class Shape : ISupportMappable
    {

        // internal values so we can access them fast  without calling properties.
        internal Matrix3 inertia = Matrix3.Identity;
        internal float mass = 1.0f;

        internal JBBox boundingBox = JBBox.LargeBox;
        public Vector3 geomCen = Vector3.Zero;

        /// <summary>
        /// Gets called when the shape changes one of the parameters.
        /// </summary>
        public event ShapeUpdatedHandler ShapeUpdated;

        /// <summary>
        /// Creates a new instance of a shape.
        /// </summary>
        public Shape()
        {
        }

        /// <summary>
        /// Returns the inertia of the untransformed shape.
        /// </summary>
        public Matrix3 Inertia { get { return inertia; } protected set { inertia = value; } }


        /// <summary>
        /// Gets the mass of the shape. This is the volume. (density = 1)
        /// </summary>
        public float Mass { get { return mass; } protected set { mass = value; } }

        /// <summary>
        /// Informs all listener that the shape changed.
        /// </summary>
        protected void RaiseShapeUpdated()
        {
            if (ShapeUpdated != null) ShapeUpdated();
        }

        /// <summary>
        /// The untransformed axis aligned bounding box of the shape.
        /// </summary>
        public JBBox BoundingBox { get { return boundingBox; } }

        /// <summary>
        /// Allows to set a user defined value to the shape.
        /// </summary>
        public object Tag { get; set; }

        private struct ClipTriangle
        {
            public Vector3 n1;
            public Vector3 n2;
            public Vector3 n3;
            public int generation;
        };

        /// <summary>
        /// Hull making.
        /// </summary>
        /// <remarks>Based/Completely from http://www.xbdev.net/physics/MinkowskiDifference/index.php
        /// I don't (100%) see why this should always work.
        /// </remarks>
        /// <param name="triangleList"></param>
        /// <param name="generationThreshold"></param>
        public virtual void MakeHull(ref List<Vector3> triangleList, int generationThreshold)
        {
            float distanceThreshold = 0.0f;

            if (generationThreshold < 0) generationThreshold = 4;

            Stack<ClipTriangle> activeTriList = new Stack<ClipTriangle>();

            Vector3[] v = new Vector3[] // 6 Array
		    {
			new Vector3( -1,  0,  0 ),
			new Vector3(  1,  0,  0 ),

			new Vector3(  0, -1,  0 ),
			new Vector3(  0,  1,  0 ),

			new Vector3(  0,  0, -1 ),
			new Vector3(  0,  0,  1 ),
		    };

            int[,] kTriangleVerts = new int[8, 3] // 8 x 3 Array
		    {
			{ 5, 1, 3 },
			{ 4, 3, 1 },
			{ 3, 4, 0 },
			{ 0, 5, 3 },

			{ 5, 2, 1 },
			{ 4, 1, 2 },
			{ 2, 0, 4 },
			{ 0, 2, 5 }
		    };

            for (int i = 0; i < 8; i++)
            {
                ClipTriangle tri = new ClipTriangle();
                tri.n1 = v[kTriangleVerts[i, 0]];
                tri.n2 = v[kTriangleVerts[i, 1]];
                tri.n3 = v[kTriangleVerts[i, 2]];
                tri.generation = 0;
                activeTriList.Push(tri);
            }

            List<Vector3> pointSet = new List<Vector3>();

            // surfaceTriList
            while (activeTriList.Count > 0)
            {
                ClipTriangle tri = activeTriList.Pop();

                Vector3 p1; SupportMapping(ref tri.n1, out p1);
                Vector3 p2; SupportMapping(ref tri.n2, out p2);
                Vector3 p3; SupportMapping(ref tri.n3, out p3);

                float d1 = (p2 - p1).LengthSquared;
                float d2 = (p3 - p2).LengthSquared;
                float d3 = (p1 - p3).LengthSquared;

                if (Math.Max(Math.Max(d1, d2), d3) > distanceThreshold && tri.generation < generationThreshold)
                {
                    ClipTriangle tri1 = new ClipTriangle();
                    ClipTriangle tri2 = new ClipTriangle();
                    ClipTriangle tri3 = new ClipTriangle();
                    ClipTriangle tri4 = new ClipTriangle();

                    tri1.generation = tri.generation + 1;
                    tri2.generation = tri.generation + 1;
                    tri3.generation = tri.generation + 1;
                    tri4.generation = tri.generation + 1;

                    tri1.n1 = tri.n1;
                    tri2.n2 = tri.n2;
                    tri3.n3 = tri.n3;

                    Vector3 n = 0.5f * (tri.n1 + tri.n2);
                    n.Normalize();

                    tri1.n2 = n;
                    tri2.n1 = n;
                    tri4.n3 = n;

                    n = 0.5f * (tri.n2 + tri.n3);
                    n.Normalize();

                    tri2.n3 = n;
                    tri3.n2 = n;
                    tri4.n1 = n;

                    n = 0.5f * (tri.n3 + tri.n1);
                    n.Normalize();

                    tri1.n3 = n;
                    tri3.n1 = n;
                    tri4.n2 = n;

                    activeTriList.Push(tri1);
                    activeTriList.Push(tri2);
                    activeTriList.Push(tri3);
                    activeTriList.Push(tri4);
                }
                else
                {
                    if (Vector3.Cross((p3 - p1) , (p2 - p1)).LengthSquared > JMath.Epsilon)
                    {
                        triangleList.Add(p1);
                        triangleList.Add(p2);
                        triangleList.Add(p3);
                    }
                }
            }
        }


        /// <summary>
        /// Uses the supportMapping to calculate the bounding box. Should be overidden
        /// to make this faster.
        /// </summary>
        /// <param name="orientation">The orientation of the shape.</param>
        /// <param name="box">The resulting axis aligned bounding box.</param>
        public virtual void GetBoundingBox(ref Matrix3 orientation, out JBBox box)
        {
            // I don't think that this can be done faster.
            // 6 is the minimum number of SupportMap calls.

            Vector3 vec = Vector3.Zero;

            vec.Set(orientation.M11, orientation.M21, orientation.M31);
            SupportMapping(ref vec, out vec);
            box.Max.X = orientation.M11 * vec.X + orientation.M21 * vec.Y + orientation.M31 * vec.Z;

            vec.Set(orientation.M12, orientation.M22, orientation.M32);
            SupportMapping(ref vec, out vec);
            box.Max.Y = orientation.M12 * vec.X + orientation.M22 * vec.Y + orientation.M32 * vec.Z;

            vec.Set(orientation.M13, orientation.M23, orientation.M33);
            SupportMapping(ref vec, out vec);
            box.Max.Z = orientation.M13 * vec.X + orientation.M23 * vec.Y + orientation.M33 * vec.Z;

            vec.Set(-orientation.M11, -orientation.M21, -orientation.M31);
            SupportMapping(ref vec, out vec);
            box.Min.X = orientation.M11 * vec.X + orientation.M21 * vec.Y + orientation.M31 * vec.Z;

            vec.Set(-orientation.M12, -orientation.M22, -orientation.M32);
            SupportMapping(ref vec, out vec);
            box.Min.Y = orientation.M12 * vec.X + orientation.M22 * vec.Y + orientation.M32 * vec.Z;

            vec.Set(-orientation.M13, -orientation.M23, -orientation.M33);
            SupportMapping(ref vec, out vec);
            box.Min.Z = orientation.M13 * vec.X + orientation.M23 * vec.Y + orientation.M33 * vec.Z;
        }

        /// <summary>
        /// This method uses the <see cref="ISupportMappable"/> implementation
        /// to calculate the local bounding box, the mass, geometric center and 
        /// the inertia of the shape. In custom shapes this method should be overidden
        /// to compute this values faster.
        /// </summary>
        public virtual void UpdateShape()
        {
            GetBoundingBox(ref Matrix3.Identity, out boundingBox);

            CalculateMassInertia();
            RaiseShapeUpdated();
        }
        
        /// <summary>
        /// Calculates the inertia of the shape relative to the center of mass.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="centerOfMass"></param>
        /// <param name="inertia">Returns the inertia relative to the center of mass, not to the origin</param>
        /// <returns></returns>
        #region  public static float CalculateMassInertia(Shape shape, out JVector centerOfMass, out JMatrix inertia)
        public static float CalculateMassInertia(Shape shape, out Vector3 centerOfMass,
            out Matrix3 inertia)
        {
            float mass = 0.0f;
            centerOfMass = Vector3.Zero; inertia = Matrix3.Zero;

            if (shape is Multishape) throw new ArgumentException("Can't calculate inertia of multishapes.", "shape");

            // build a triangle hull around the shape
            List<Vector3> hullTriangles = new List<Vector3>();
            shape.MakeHull(ref hullTriangles, 3);

            // create inertia of tetrahedron with vertices at
            // (0,0,0) (1,0,0) (0,1,0) (0,0,1)
            float a = 1.0f / 60.0f, b = 1.0f / 120.0f;
            Matrix3 C = new Matrix3(a, b, b, b, a, b, b, b, a);

            for (int i = 0; i < hullTriangles.Count; i += 3)
            {
                Vector3 column0 = hullTriangles[i + 0];
                Vector3 column1 = hullTriangles[i + 1];
                Vector3 column2 = hullTriangles[i + 2];

                Matrix3 A = new Matrix3(column0.X, column1.X, column2.X,
                    column0.Y, column1.Y, column2.Y,
                    column0.Z, column1.Z, column2.Z);

                float detA = A.Determinant;

				// now transform this canonical tetrahedron to the target tetrahedron
				// inertia by a linear transformation A
				var acta = A * C * Matrix3.Transpose(A);
				Matrix3 tetrahedronInertia; Matrix3.Mult(ref acta, detA, out tetrahedronInertia);

                Vector3 tetrahedronCOM = (1.0f / 4.0f) * (hullTriangles[i + 0] + hullTriangles[i + 1] + hullTriangles[i + 2]);
                float tetrahedronMass = (1.0f / 6.0f) * detA;

				inertia = Matrix3.Add(inertia, tetrahedronInertia);
                centerOfMass += tetrahedronMass * tetrahedronCOM;
                mass += tetrahedronMass;
            }
			//
			Matrix3 tmpIn;
			Matrix3.Mult(ref Matrix3.Identity, inertia.Trace, out tmpIn);
			inertia = Matrix3.Sub(tmpIn, inertia);
            //inertia = Matrix3.Multiply(Matrix3.Identity, inertia.Trace) - inertia;
            centerOfMass = centerOfMass * (1.0f / mass);

            float x = centerOfMass.X;
            float y = centerOfMass.Y;
            float z = centerOfMass.Z;

            // now translate the inertia by the center of mass
            Matrix3 t = new Matrix3(
                -mass * (y * y + z * z), mass * x * y, mass * x * z,
                mass * y * x, -mass * (z * z + x * x), mass * y * z,
                mass * z * x, mass * z * y, -mass * (x * x + y * y));

            Matrix3.Add(ref inertia, ref t, out inertia);

            return mass;
        }
        #endregion
        
        /// <summary>
        /// Numerically calculates the inertia, mass and geometric center of the shape.
        /// This gets a good value for "normal" shapes. The algorithm isn't very accurate
        /// for very flat shapes. 
        /// </summary>
        public virtual void CalculateMassInertia()
        {
            this.mass = Shape.CalculateMassInertia(this, out geomCen, out inertia);
        }

        /// <summary>
        /// SupportMapping. Finds the point in the shape furthest away from the given direction.
        /// Imagine a plane with a normal in the search direction. Now move the plane along the normal
        /// until the plane does not intersect the shape. The last intersection point is the result.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="result">The result.</param>
        public abstract void SupportMapping(ref Vector3 direction, out Vector3 result);

        /// <summary>
        /// The center of the SupportMap.
        /// </summary>
        /// <param name="geomCenter">The center of the SupportMap.</param>
        public void SupportCenter(out Vector3 geomCenter)
        {
            geomCenter = this.geomCen;
        }

    }
}
