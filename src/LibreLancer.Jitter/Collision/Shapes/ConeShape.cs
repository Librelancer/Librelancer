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
    /// A <see cref="Shape"/> representing a cone.
    /// </summary>
    public class ConeShape : Shape
    {
        float height,radius;

        /// <summary>
        /// The height of the cone.
        /// </summary>
        public float Height { get { return height; } set { height = value; UpdateShape(); } }

        /// <summary>
        /// The radius of the cone base.
        /// </summary>
        public float Radius { get { return radius; } set { radius = value; UpdateShape(); } }

        /// <summary>
        /// Initializes a new instance of the ConeShape class.
        /// </summary>
        /// <param name="height">The height of the cone.</param>
        /// <param name="radius">The radius of the cone base.</param>
        public ConeShape(float height, float radius)
        {
            this.height = height;
            this.radius = radius;

            this.UpdateShape();
        }

        public override void UpdateShape()
        {
            sina = radius / (float)Math.Sqrt(radius * radius + height * height);
            base.UpdateShape();
        }

        float sina = 0.0f;

        /// <summary>
        /// 
        /// </summary>
        public override void CalculateMassInertia()
        {
            mass = (1.0f / 3.0f) * JMath.Pi * radius * radius * height;

            // inertia through center of mass axis.
            inertia = Matrix3.Identity;
            inertia.M11 = (3.0f / 80.0f) * mass * (radius * radius + 4 * height * height);
            inertia.M22 = (3.0f / 10.0f) * mass * radius * radius;
            inertia.M33 = (3.0f / 80.0f) * mass * (radius * radius + 4 * height * height);

            // J_x=J_y=3/20 M (R^2+4 H^2)

            // the supportmap center is in the half height, the real geomcenter is:
            geomCen = Vector3.Zero;
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
            float sigma = (float)Math.Sqrt((float)(direction.X * direction.X + direction.Z * direction.Z));

            if (direction.Y > direction.Length * sina)
            {
                result.X = 0.0f;
                result.Y = (2.0f / 3.0f) * height;
                result.Z = 0.0f;
            }
            else if (sigma > 0.0f)
            {
                result.X = radius * direction.X / sigma;
                result.Y = -(1.0f / 3.0f) * height;
                result.Z = radius * direction.Z / sigma;
            }
            else
            {
                result.X = 0.0f;
                result.Y = -(1.0f / 3.0f) * height;
                result.Z = 0.0f;
            }

        }
    }
}
