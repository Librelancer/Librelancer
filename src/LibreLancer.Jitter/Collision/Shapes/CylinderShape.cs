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
    /// A <see cref="Shape"/> representing a cylinder.
    /// </summary>
    public class CylinderShape : Shape
    {
        private float height, radius;

        /// <summary>
        /// Sets the height of the cylinder.
        /// </summary>
        public float Height { get { return height; } set { height = value; UpdateShape(); } }

        /// <summary>
        /// Sets the radius of the cylinder.
        /// </summary>
        public float Radius { get { return radius; } set { radius = value; UpdateShape(); } }

        /// <summary>
        /// Initializes a new instance of the CylinderShape class.
        /// </summary>
        /// <param name="height">The height of the cylinder.</param>
        /// <param name="radius">The radius of the cylinder.</param>
        public CylinderShape(float height, float radius)
        {
            this.height = height;
            this.radius = radius;
            UpdateShape();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void CalculateMassInertia()
        {
            this.mass = JMath.Pi * radius * radius * height;
            this.inertia.M11 = (1.0f / 4.0f) * mass * radius * radius + (1.0f / 12.0f) * mass * height * height;
            this.inertia.M22 = (1.0f / 2.0f) * mass * radius * radius;
            this.inertia.M33 = (1.0f / 4.0f) * mass * radius * radius + (1.0f / 12.0f) * mass * height * height;
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

            if (sigma > 0.0f)
            {
                result.X = direction.X / sigma * radius;
                result.Y = (float)Math.Sign(direction.Y) * height * 0.5f;
                result.Z = direction.Z / sigma * radius;
            }
            else
            {
                result.X = 0.0f;
                result.Y = (float)Math.Sign(direction.Y) * height * 0.5f;
                result.Z = 0.0f;
            }
        }
    }
}
