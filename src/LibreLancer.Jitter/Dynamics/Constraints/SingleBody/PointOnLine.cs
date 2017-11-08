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

namespace LibreLancer.Jitter.Dynamics.Constraints.SingleBody
{

    /// <summary>
    /// </summary>
    public class PointOnLine : Constraint
    {
        private Vector3 localAnchor1;
        private Vector3 r1;

        private Vector3 lineNormal = Vector3.Right;
        private Vector3 anchor;

        private float biasFactor = 0.5f;
        private float softness = 0.0f;

        /// <summary>
        /// Initializes a new instance of the WorldLineConstraint.
        /// </summary>
        /// <param name="body">The body of the constraint.</param>
        /// <param name="localAnchor">The anchor point on the body in local (body)
        /// coordinates.</param>
        /// <param name="lineDirection">The axis defining the line in world space.</param>/param>
        public PointOnLine(RigidBody body, Vector3 localAnchor, Vector3 lineDirection)
            : base(body, null)
        {
            if (lineDirection.LengthSquared == 0.0f)
                throw new ArgumentException("Line direction can't be zero", "lineDirection");

            localAnchor1 = localAnchor;
            this.anchor = body.position + Vector3.Transform(localAnchor, body.orientation);

            this.lineNormal = lineDirection;
            this.lineNormal.Normalize();
        }

        /// <summary>
        /// The anchor point of the body in world space.
        /// </summary>
        public Vector3 Anchor { get { return anchor; } set { anchor = value; } }

        /// <summary>
        /// The axis defining the line of the constraint.
        /// </summary>
        public Vector3 Axis { get { return lineNormal; } set { lineNormal = value; lineNormal.Normalize(); } }

        /// <summary>
        /// Defines how big the applied impulses can get.
        /// </summary>
        public float Softness { get { return softness; } set { softness = value; } }

        /// <summary>
        /// Defines how big the applied impulses can get which correct errors.
        /// </summary>
        public float BiasFactor { get { return biasFactor; } set { biasFactor = value; } }

        float effectiveMass = 0.0f;
        float accumulatedImpulse = 0.0f;
        float bias;
        float softnessOverDt;

        Vector3[] jacobian = new Vector3[2];

        /// <summary>
        /// Called once before iteration starts.
        /// </summary>
        /// <param name="timestep">The simulation timestep</param>
        public override void PrepareForIteration(float timestep)
        {
            Vector3.Transform(ref localAnchor1, ref body1.orientation, out r1);

            Vector3 p1, dp;
            Vector3.Add(ref body1.position, ref r1, out p1);

            Vector3.Subtract(ref p1, ref anchor, out dp);

            Vector3 l = lineNormal;

			Vector3 t = Vector3.Cross((p1 - anchor), l);
            if (t.LengthSquared != 0.0f) t.Normalize();
			t = Vector3.Cross(t , l);

            jacobian[0] = t;
			jacobian[1] = Vector3.Cross(r1, t);

            effectiveMass = body1.inverseMass
			                     + Vector3.Dot(Vector3.Transform(jacobian[1], body1.invInertiaWorld), jacobian[1]);

            softnessOverDt = softness / timestep;
            effectiveMass += softnessOverDt;

            if (effectiveMass != 0) effectiveMass = 1.0f / effectiveMass;

			bias = -(Vector3.Cross(l, (p1 - anchor))).Length * biasFactor * (1.0f / timestep);

            if (!body1.isStatic)
            {
                body1.linearVelocity += body1.inverseMass * accumulatedImpulse * jacobian[0];
                body1.angularVelocity += Vector3.Transform(accumulatedImpulse * jacobian[1], body1.invInertiaWorld);
            }

        }

        /// <summary>
        /// Iteratively solve this constraint.
        /// </summary>
        public override void Iterate()
        {
            float jv =
				Vector3.Dot(body1.linearVelocity, jacobian[0]) +
				Vector3.Dot(body1.angularVelocity, jacobian[1]);

            float softnessScalar = accumulatedImpulse * softnessOverDt;

            float lambda = -effectiveMass * (jv + bias + softnessScalar);

            accumulatedImpulse += lambda;

            if (!body1.isStatic)
            {
                body1.linearVelocity += body1.inverseMass * lambda * jacobian[0];
                body1.angularVelocity += Vector3.Transform(lambda * jacobian[1], body1.invInertiaWorld);
            }
        }

        public override void DebugDraw(IDebugDrawer drawer)
        {
            drawer.DrawLine(anchor - lineNormal * 50.0f, anchor + lineNormal * 50.0f);
            drawer.DrawLine(body1.position, body1.position + r1);
        }

    }
}
