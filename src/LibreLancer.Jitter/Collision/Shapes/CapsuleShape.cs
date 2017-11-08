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
    /// A <see cref="Shape"/> representing a capsule.
    /// </summary>
    public class CapsuleShape : Shape
    {
        private float length, radius;

        /// <summary>
        /// Gets or sets the length of the capsule (exclusive the round endcaps).
        /// </summary>
        public float Length { get { return length; } set { length = value; UpdateShape(); } }

        /// <summary>
        /// Gets or sets the radius of the endcaps.
        /// </summary>
        public float Radius { get { return radius; } set { radius = value; UpdateShape(); } }

        /// <summary>
        /// Create a new instance of the capsule.
        /// </summary>
        /// <param name="length">The length of the capsule (exclusive the round endcaps).</param>
        /// <param name="radius">The radius of the endcaps.</param>
        public CapsuleShape(float length,float radius)
        {
            this.length = length;
            this.radius = radius;
            UpdateShape();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void CalculateMassInertia()
        {
            float massSphere = (3.0f / 4.0f) * JMath.Pi * radius * radius * radius;
            float massCylinder = JMath.Pi * radius * radius * length;

            mass = massCylinder + massSphere;

            this.inertia.M11 = (1.0f / 4.0f) * massCylinder * radius * radius + (1.0f / 12.0f) * massCylinder * length * length + (2.0f / 5.0f) * massSphere * radius * radius + (1.0f / 4.0f) * length * length * massSphere;
            this.inertia.M22 = (1.0f / 2.0f) * massCylinder * radius * radius + (2.0f / 5.0f) * massSphere * radius * radius;
            this.inertia.M33 = (1.0f / 4.0f) * massCylinder * radius * radius + (1.0f / 12.0f) * massCylinder * length * length + (2.0f / 5.0f) * massSphere * radius * radius + (1.0f / 4.0f) * length * length * massSphere;

            //this.inertia.M11 = (1.0f / 4.0f) * mass * radius * radius + (1.0f / 12.0f) * mass * height * height;
            //this.inertia.M22 = (1.0f / 2.0f) * mass * radius * radius;
            //this.inertia.M33 = (1.0f / 4.0f) * mass * radius * radius + (1.0f / 12.0f) * mass * height * height;
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
            float r = (float)Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z);

            if (Math.Abs(direction.Y) > 0.0f)
            {
                Vector3 dir; Vector3.Normalize(ref direction, out dir);
                Vector3.Multiply(ref dir, radius, out result);
                result.Y += Math.Sign(direction.Y) * 0.5f * length;              
            }
            else if (r > 0.0f)
            {
                result.X = direction.X / r * radius;
                result.Y = 0.0f;
                result.Z = direction.Z / r * radius;
            }
            else
            {
                result.X = 0.0f;
                result.Y = 0.0f;
                result.Z = 0.0f;
            }
        }
    }
}
