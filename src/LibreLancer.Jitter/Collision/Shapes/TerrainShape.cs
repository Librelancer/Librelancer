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
    /// Represents a terrain.
    /// </summary>
    public class TerrainShape : Multishape
    {
        private float[,] heights;
        private float scaleX, scaleZ;
        private int heightsLength0, heightsLength1;

        private int minX, maxX;
        private int minZ, maxZ;
        private int numX, numZ;

        private JBBox boundings;

        private float sphericalExpansion = 0.05f;

        /// <summary>
        /// Expands the triangles by the specified amount.
        /// This stabilizes collision detection for flat shapes.
        /// </summary>
        public float SphericalExpansion
        {
            get { return sphericalExpansion; }
            set { sphericalExpansion = value; }
        }

        /// <summary>
        /// Initializes a new instance of the TerrainShape class.
        /// </summary>
        /// <param name="heights">An array containing the heights of the terrain surface.</param>
        /// <param name="scaleX">The x-scale factor. (The x-space between neighbour heights)</param>
        /// <param name="scaleZ">The y-scale factor. (The y-space between neighbour heights)</param>
        public TerrainShape(float[,] heights, float scaleX, float scaleZ)
        {
            heightsLength0 = heights.GetLength(0);
            heightsLength1 = heights.GetLength(1);

            #region Bounding Box
            boundings = JBBox.SmallBox;

            for (int i = 0; i < heightsLength0; i++)
            {
                for (int e = 0; e < heightsLength1; e++)
                {
                    if (heights[i, e] > boundings.Max.Y)
                        boundings.Max.Y = heights[i, e];
                    else if (heights[i, e] < boundings.Min.Y)
                        boundings.Min.Y = heights[i, e];
                }
            }

            boundings.Min.X = 0.0f;
            boundings.Min.Z = 0.0f;

            boundings.Max.X = checked(heightsLength0 * scaleX);
            boundings.Max.Z = checked(heightsLength1 * scaleZ);

            #endregion

            this.heights = heights;
            this.scaleX = scaleX;
            this.scaleZ = scaleZ;

            UpdateShape();
        }

        internal TerrainShape() { }

 
        protected override Multishape CreateWorkingClone()
        {
            TerrainShape clone = new TerrainShape();
            clone.heights = this.heights;
            clone.scaleX = this.scaleX;
            clone.scaleZ = this.scaleZ;
            clone.boundings = this.boundings;
            clone.heightsLength0 = this.heightsLength0;
            clone.heightsLength1 = this.heightsLength1;
            clone.sphericalExpansion = this.sphericalExpansion;
            return clone;
        }


        private Vector3[] points = new Vector3[3];
        private Vector3 normal = Vector3.Up;

        /// <summary>
        /// Sets the current shape. First <see cref="Prepare"/> has to be called.
        /// After SetCurrentShape the shape immitates another shape.
        /// </summary>
        /// <param name="index"></param>
        public override void SetCurrentShape(int index)
        {
            bool leftTriangle = false;

            if (index >= numX * numZ)
            {
                leftTriangle = true;
                index -= numX * numZ;
            }

            int quadIndexX = index % numX;
            int quadIndexZ = index / numX;

            // each quad has two triangles, called 'leftTriangle' and !'leftTriangle'
            if (leftTriangle)
            {
                points[0].Set((minX + quadIndexX + 0) * scaleX, heights[minX + quadIndexX + 0, minZ + quadIndexZ + 0], (minZ + quadIndexZ + 0) * scaleZ);
                points[1].Set((minX + quadIndexX + 1) * scaleX, heights[minX + quadIndexX + 1, minZ + quadIndexZ + 0], (minZ + quadIndexZ + 0) * scaleZ);
                points[2].Set((minX + quadIndexX + 0) * scaleX, heights[minX + quadIndexX + 0, minZ + quadIndexZ + 1], (minZ + quadIndexZ + 1) * scaleZ);
            }
            else
            {
                points[0].Set((minX + quadIndexX + 1) * scaleX, heights[minX + quadIndexX + 1, minZ + quadIndexZ + 0], (minZ + quadIndexZ + 0) * scaleZ);
                points[1].Set((minX + quadIndexX + 1) * scaleX, heights[minX + quadIndexX + 1, minZ + quadIndexZ + 1], (minZ + quadIndexZ + 1) * scaleZ);
                points[2].Set((minX + quadIndexX + 0) * scaleX, heights[minX + quadIndexX + 0, minZ + quadIndexZ + 1], (minZ + quadIndexZ + 1) * scaleZ);
            }

            Vector3 sum = points[0];
            Vector3.Add(ref sum, ref points[1], out sum);
            Vector3.Add(ref sum, ref points[2], out sum);
            Vector3.Multiply(ref sum, 1.0f / 3.0f, out sum);
            geomCen = sum;

            Vector3.Subtract(ref points[1], ref points[0], out sum);
            Vector3.Subtract(ref points[2], ref points[0], out normal);
            Vector3.Cross(ref sum, ref normal, out normal);
        }

        public void CollisionNormal(out Vector3 normal)
        {
            normal = this.normal;
        }


        /// <summary>
        /// Passes a axis aligned bounding box to the shape where collision
        /// could occour.
        /// </summary>
        /// <param name="box">The bounding box where collision could occur.</param>
        /// <returns>The upper index with which <see cref="SetCurrentShape"/> can be 
        /// called.</returns>
        public override int Prepare(ref JBBox box)
        {
            // simple idea: the terrain is a grid. x and z is the position in the grid.
            // y the height. we know compute the min and max grid-points. All quads
            // between these points have to be checked.

            // including overflow exception prevention

            if (box.Min.X < boundings.Min.X) minX = 0;
            else
            {
                minX = (int)Math.Floor((float)((box.Min.X - sphericalExpansion) / scaleX));
                minX = Math.Max(minX, 0);
            }

            if (box.Max.X > boundings.Max.X) maxX = heightsLength0 - 1;
            else
            {
                maxX = (int)Math.Ceiling((float)((box.Max.X + sphericalExpansion) / scaleX));
                maxX = Math.Min(maxX, heightsLength0 - 1);
            }

            if (box.Min.Z < boundings.Min.Z) minZ = 0;
            else
            {
                minZ = (int)Math.Floor((float)((box.Min.Z - sphericalExpansion) / scaleZ));
                minZ = Math.Max(minZ, 0);
            }

            if (box.Max.Z > boundings.Max.Z) maxZ = heightsLength1 - 1;
            else
            {
                maxZ = (int)Math.Ceiling((float)((box.Max.Z + sphericalExpansion) / scaleZ));
                maxZ = Math.Min(maxZ, heightsLength1 - 1);
            }

            numX = maxX - minX;
            numZ = maxZ - minZ;

            // since every quad contains two triangles we multiply by 2.
            return numX * numZ * 2;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void CalculateMassInertia()
        {
            this.inertia = Matrix3.Identity;
            this.Mass = 1.0f;
        }

        /// <summary>
        /// Gets the axis aligned bounding box of the orientated shape. This includes
        /// the whole shape.
        /// </summary>
        /// <param name="orientation">The orientation of the shape.</param>
        /// <param name="box">The axis aligned bounding box of the shape.</param>
        public override void GetBoundingBox(ref Matrix3 orientation, out JBBox box)
        {
            box = boundings;

            #region Expand Spherical
            box.Min.X -= sphericalExpansion;
            box.Min.Y -= sphericalExpansion;
            box.Min.Z -= sphericalExpansion;
            box.Max.X += sphericalExpansion;
            box.Max.Y += sphericalExpansion;
            box.Max.Z += sphericalExpansion;
            #endregion

            box.Transform(ref orientation);
        }

        public override void MakeHull(ref List<Vector3> triangleList, int generationThreshold)
        {
            for (int index = 0; index < (heightsLength0 - 1) * (heightsLength1 - 1); index++)
            {
                int quadIndexX = index % (heightsLength0 - 1);
                int quadIndexZ = index / (heightsLength0 - 1);

                triangleList.Add(new Vector3((0 + quadIndexX + 0) * scaleX, heights[0 + quadIndexX + 0, 0 + quadIndexZ + 0], (0 + quadIndexZ + 0) * scaleZ));
                triangleList.Add(new Vector3((0 + quadIndexX + 1) * scaleX, heights[0 + quadIndexX + 1, 0 + quadIndexZ + 0], (0 + quadIndexZ + 0) * scaleZ));
                triangleList.Add(new Vector3((0 + quadIndexX + 0) * scaleX, heights[0 + quadIndexX + 0, 0 + quadIndexZ + 1], (0 + quadIndexZ + 1) * scaleZ));

                triangleList.Add(new Vector3((0 + quadIndexX + 1) * scaleX, heights[0 + quadIndexX + 1, 0 + quadIndexZ + 0], (0 + quadIndexZ + 0) * scaleZ));
                triangleList.Add(new Vector3((0 + quadIndexX + 1) * scaleX, heights[0 + quadIndexX + 1, 0 + quadIndexZ + 1], (0 + quadIndexZ + 1) * scaleZ));
                triangleList.Add(new Vector3((0 + quadIndexX + 0) * scaleX, heights[0 + quadIndexX + 0, 0 + quadIndexZ + 1], (0 + quadIndexZ + 1) * scaleZ));
            }
        }

        /// <summary>
        /// SupportMapping. Finds the point in the shape furthest away from the given direction.
        /// Imagine a plane with a normal in the search direction. Now move the plane along the normal
        /// until the plane does not intersect the shape. The last intersection point is the result.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="result">The result.</param>
        public override void SupportMapping(ref Vector3 direction, out Vector3 result)
        {
            Vector3 expandVector;
            Vector3.Normalize(ref direction, out expandVector);
            Vector3.Multiply(ref expandVector, sphericalExpansion, out expandVector);

            int minIndex = 0;
            float min = Vector3.Dot(ref points[0], ref direction);
            float dot = Vector3.Dot(ref points[1], ref direction);
            if (dot > min)
            {
                min = dot;
                minIndex = 1;
            }
            dot = Vector3.Dot(ref points[2], ref direction);
            if (dot > min)
            {
                min = dot;
                minIndex = 2;
            }

            Vector3.Add(ref points[minIndex], ref expandVector, out result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rayOrigin"></param>
        /// <param name="rayDelta"></param>
        /// <returns></returns>
        public override int Prepare(ref Vector3 rayOrigin, ref Vector3 rayDelta)
        {
            JBBox box = JBBox.SmallBox;

            #region RayEnd + Expand Spherical
            Vector3 rayEnd;
            Vector3.Normalize(ref rayDelta, out rayEnd);
            rayEnd = rayOrigin + rayDelta + rayEnd * sphericalExpansion;
            #endregion

            box.AddPoint(ref rayOrigin);
            box.AddPoint(ref rayEnd);

            return this.Prepare(ref box);
        }
    }
}
