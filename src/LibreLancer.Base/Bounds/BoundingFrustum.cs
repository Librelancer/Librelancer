
#region License
/*
MIT License
Copyright © 2006 The Mono.Xna Team

All rights reserved.

Authors:
Olivier Dufour (Duff)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace LibreLancer
{
    public struct BoundingFrustum : IEquatable<BoundingFrustum>
    {
        #region Private Fields

        private Matrix4x4 matrix;

        private Vector3 corner0;
        private Vector3 corner1;
        private Vector3 corner2;
        private Vector3 corner3;
        private Vector3 corner4;
        private Vector3 corner5;
        private Vector3 corner6;
        private Vector3 corner7;

        private Plane plane0;
        private Plane plane1;
        private Plane plane2;
        private Plane plane3;
        private Plane plane4;
        private Plane plane5;


        //private readonly Vector3[] corners = new Vector3[CornerCount];
        //private readonly Plane[] planes = new Plane[PlaneCount];

        private const int PlaneCount = 6;

        #endregion Private Fields

        #region Public Fields
        public const int CornerCount = 8;
        #endregion

        #region Public Constructors

        public BoundingFrustum(Matrix4x4 value)
        {
            this.matrix = value;
            this.CreatePlanes();
            this.CreateCorners();
        }

        #endregion Public Constructors


        #region Public Properties

        public Matrix4x4 Matrix4x4
        {
            get { return this.matrix; }
            set
            {
                this.matrix = value;
                this.CreatePlanes();    // FIXME: The odds are the planes will be used a lot more often than the matrix
            	this.CreateCorners();   // is updated, so this should help performance. I hope ;)
			}
        }

        public Plane Near
        {
            get { return plane0; }
        }

        public Plane Far
        {
            get { return plane1; }
        }

        public Plane Left
        {
            get { return plane2; }
        }

        public Plane Right
        {
            get { return plane3; }
        }

        public Plane Top
        {
            get { return plane4; }
        }

        public Plane Bottom
        {
            get { return plane5; }
        }

        #endregion Public Properties


        #region Public Methods

        public static bool operator ==(BoundingFrustum a, BoundingFrustum b)
        {
            if (object.Equals(a, null))
                return (object.Equals(b, null));

            if (object.Equals(b, null))
                return (object.Equals(a, null));

            return a.matrix == (b.matrix);
        }

        public static bool operator !=(BoundingFrustum a, BoundingFrustum b)
        {
            return !(a == b);
        }

        public ContainmentType Contains(BoundingBox box)
        {
            var result = default(ContainmentType);
            this.Contains(ref box, out result);
            return result;
        }

        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            bool intersects = false;
            box.Intersects(ref plane0, out var intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            box.Intersects(ref plane1, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            box.Intersects(ref plane2, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            box.Intersects(ref plane3, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            box.Intersects(ref plane4, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            box.Intersects(ref plane5, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            result = intersects ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        public ContainmentType Contains(BoundingSphere sphere)
        {
            var result = default(ContainmentType);
            this.Contains(ref sphere, out result);
            return result;
        }

        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            bool intersects = false;
            sphere.Intersects(ref plane0, out var intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            sphere.Intersects(ref plane1, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            sphere.Intersects(ref plane2, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            sphere.Intersects(ref plane3, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            sphere.Intersects(ref plane4, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            sphere.Intersects(ref plane5, out intersectionType);
            if (intersectionType == PlaneIntersectionType.Front) {
                result = ContainmentType.Disjoint;
                return;
            }
            else if (intersectionType == PlaneIntersectionType.Intersecting) {
                intersects = true;
            }
            result = intersects ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        public ContainmentType Contains(Vector3 point)
        {
            var result = default(ContainmentType);
            this.Contains(ref point, out result);
            return result;
        }

        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            if (PlaneHelper.ClassifyPoint(ref point, ref plane0) > 0) {
                result = ContainmentType.Disjoint;
                return;
            }
            if (PlaneHelper.ClassifyPoint(ref point, ref plane1) > 0) {
                result = ContainmentType.Disjoint;
                return;
            }if (PlaneHelper.ClassifyPoint(ref point, ref plane2) > 0) {
                result = ContainmentType.Disjoint;
                return;
            }if (PlaneHelper.ClassifyPoint(ref point, ref plane3) > 0) {
                result = ContainmentType.Disjoint;
                return;
            }if (PlaneHelper.ClassifyPoint(ref point, ref plane4) > 0) {
                result = ContainmentType.Disjoint;
                return;
            }if (PlaneHelper.ClassifyPoint(ref point, ref plane5) > 0) {
                result = ContainmentType.Disjoint;
                return;
            }
            result = ContainmentType.Contains;
        }

        public bool Equals(BoundingFrustum other)
        {
            return (this == other);
        }

        public override bool Equals(object obj)
        {
            return obj is BoundingFrustum bf && this == bf;
        }

		public void GetCorners(Span<Vector3> corners)
        {
			if (corners == null) throw new ArgumentNullException("corners");
		    if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException("corners");

            corners[0] = corner0;
            corners[1] = corner1;
            corners[2] = corner2;
            corners[3] = corner3;
            corners[4] = corner4;
            corners[5] = corner5;
            corners[6] = corner6;
            corners[7] = corner7;
        }

        public override int GetHashCode()
        {
            return this.matrix.GetHashCode();
        }

        public bool Intersects(BoundingBox box)
        {
			var result = false;
			this.Intersects(ref box, out result);
			return result;
        }

        public void Intersects(ref BoundingBox box, out bool result)
        {
			var containment = default(ContainmentType);
			this.Contains(ref box, out containment);
			result = containment != ContainmentType.Disjoint;
		}

        /*
        public bool Intersects(BoundingFrustum frustum)
        {
            throw new NotImplementedException();
        }
        */

        public bool Intersects(BoundingSphere sphere)
        {
            var result = default(bool);
            this.Intersects(ref sphere, out result);
            return result;
        }

        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            var containment = default(ContainmentType);
            this.Contains(ref sphere, out containment);
            result = containment != ContainmentType.Disjoint;
        }

        /*
        public PlaneIntersectionType Intersects(Plane plane)
        {
            throw new NotImplementedException();
        }

        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            throw new NotImplementedException();
        }

        public Nullable<float> Intersects(Ray ray)
        {
            throw new NotImplementedException();
        }

        public void Intersects(ref Ray ray, out Nullable<float> result)
        {
            throw new NotImplementedException();
        }
        */

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.Append("{Near:");
            sb.Append(plane0.ToString());
            sb.Append(" Far:");
            sb.Append(plane1.ToString());
            sb.Append(" Left:");
            sb.Append(plane2.ToString());
            sb.Append(" Right:");
            sb.Append(plane3.ToString());
            sb.Append(" Top:");
            sb.Append(plane4.ToString());
            sb.Append(" Bottom:");
            sb.Append(plane5.ToString());
            sb.Append("}");
            return sb.ToString();
        }

        #endregion Public Methods


        #region Private Methods

        private void CreateCorners()
        {
            IntersectionPoint(ref plane0, ref plane2, ref plane4, out corner0);
            IntersectionPoint(ref plane0, ref plane3, ref plane4, out corner1);
            IntersectionPoint(ref plane0, ref plane3, ref plane5, out corner2);
            IntersectionPoint(ref plane0, ref plane2, ref plane5, out corner3);
            IntersectionPoint(ref plane1, ref plane2, ref plane4, out corner4);
            IntersectionPoint(ref plane1, ref plane3, ref plane4, out corner5);
            IntersectionPoint(ref plane1, ref plane3, ref plane5, out corner6);
            IntersectionPoint(ref plane1, ref plane2, ref plane5, out corner7);
        }

        private void CreatePlanes()
        {
            plane0 = new Plane(-this.matrix.M13, -this.matrix.M23, -this.matrix.M33, -this.matrix.M43);
            plane1 = new Plane(this.matrix.M13 - this.matrix.M14, this.matrix.M23 - this.matrix.M24, this.matrix.M33 - this.matrix.M34, this.matrix.M43 - this.matrix.M44);
            plane2 = new Plane(-this.matrix.M14 - this.matrix.M11, -this.matrix.M24 - this.matrix.M21, -this.matrix.M34 - this.matrix.M31, -this.matrix.M44 - this.matrix.M41);
            plane3 = new Plane(this.matrix.M11 - this.matrix.M14, this.matrix.M21 - this.matrix.M24, this.matrix.M31 - this.matrix.M34, this.matrix.M41 - this.matrix.M44);
            plane4 = new Plane(this.matrix.M12 - this.matrix.M14, this.matrix.M22 - this.matrix.M24, this.matrix.M32 - this.matrix.M34, this.matrix.M42 - this.matrix.M44);
            plane5 = new Plane(-this.matrix.M14 - this.matrix.M12, -this.matrix.M24 - this.matrix.M22, -this.matrix.M34 - this.matrix.M32, -this.matrix.M44 - this.matrix.M42);

            this.NormalizePlane(ref plane0);
            this.NormalizePlane(ref plane1);
            this.NormalizePlane(ref plane2);
            this.NormalizePlane(ref plane3);
            this.NormalizePlane(ref plane4);
            this.NormalizePlane(ref plane5);
        }

        private static void IntersectionPoint(ref Plane a, ref Plane b, ref Plane c, out Vector3 result)
        {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product

            Vector3 v1, v2, v3;
            Vector3 cross;

            cross = Vector3.Cross(b.Normal, c.Normal);

            float f = Vector3.Dot(a.Normal, cross);
            f *= -1.0f;

            v1 = (a.D * (Vector3.Cross(b.Normal, c.Normal)));
            v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));
            v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

            result.X = (v1.X + v2.X + v3.X) / f;
            result.Y = (v1.Y + v2.Y + v3.Y) / f;
            result.Z = (v1.Z + v2.Z + v3.Z) / f;
        }

        private void NormalizePlane(ref Plane p)
        {
            float factor = 1f / p.Normal.Length();
            p.Normal.X *= factor;
            p.Normal.Y *= factor;
            p.Normal.Z *= factor;
            p.D *= factor;
        }

        #endregion
    }
}

