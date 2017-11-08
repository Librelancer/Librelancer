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
    /// A <see cref="Shape"/> representing a sphere.
    /// </summary>
    public class SphereShape : Shape
    {
        private float radius = 1.0f;

        /// <summary>
        /// The radius of the sphere.
        /// </summary>
        public float Radius { get { return radius; } set { radius = value; UpdateShape(); } }

        /// <summary>
        /// Creates a new instance of the SphereShape class.
        /// </summary>
        /// <param name="radius">The radius of the sphere</param>
        public SphereShape(float radius)
        {
            this.radius = radius;
            this.UpdateShape();
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
            result = direction;
            result.Normalize();

            Vector3.Multiply(ref result, radius, out result);
        }

        /// <summary>
        /// Calculates the bounding box of the sphere.
        /// </summary>
        /// <param name="orientation">The orientation of the shape.</param>
        /// <param name="box">The resulting axis aligned bounding box.</param>
        public override void GetBoundingBox(ref Matrix3 orientation, out JBBox box)
        {
            box.Min.X = -radius;
            box.Min.Y = -radius;
            box.Min.Z = -radius;
            box.Max.X = radius;
            box.Max.Y = radius;
            box.Max.Z = radius;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void CalculateMassInertia()
        {
            mass = (4.0f / 3.0f) * JMath.Pi * radius * radius * radius;

            // (0,0,0) is the center of mass, so only
            // the main matrix elements are != 0
            inertia = Matrix3.Identity;
            inertia.M11 = 0.4f * this.mass * radius * radius;
            inertia.M22 = 0.4f * this.mass * radius * radius;
            inertia.M33 = 0.4f * this.mass * radius * radius;
        }


    }
    
}
