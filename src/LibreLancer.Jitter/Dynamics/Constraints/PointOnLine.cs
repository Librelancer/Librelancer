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

namespace LibreLancer.Jitter.Dynamics.Constraints
{

    #region Constraint Equations
    // Constraint formulation:
    // 
    // C = |(p1-p2) x l|
    //
    #endregion

    /// <summary>
    /// Constraints a point on a body to be fixed on a line
    /// which is fixed on another body.
    /// </summary>
    public class PointOnLine : Constraint
    {
        private Vector3 lineNormal;

        private Vector3 localAnchor1, localAnchor2;
        private Vector3 r1, r2;

        private float biasFactor = 0.5f;
        private float softness = 0.0f;

        /// <summary>
        /// Constraints a point on a body to be fixed on a line
        /// which is fixed on another body.
        /// </summary>
        /// <param name="body1"></param>
        /// <param name="body2"></param>
        /// <param name="lineStartPointBody1"></param>
        /// <param name="lineDirection"></param>
        /// <param name="pointBody2"></param>
        public PointOnLine(RigidBody body1, RigidBody body2,
            Vector3 lineStartPointBody1, Vector3 pointBody2) : base(body1,body2)
        {

            Vector3.Subtract(ref lineStartPointBody1, ref body1.position, out localAnchor1);
            Vector3.Subtract(ref pointBody2, ref body2.position, out localAnchor2);

            Vector3.Transform(ref localAnchor1, ref body1.invOrientation, out localAnchor1);
            Vector3.Transform(ref localAnchor2, ref body2.invOrientation, out localAnchor2);

            lineNormal = Vector3.Normalize(lineStartPointBody1 - pointBody2);
        }

        public float AppliedImpulse { get { return accumulatedImpulse; } }

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

        Vector3[] jacobian = new Vector3[4];

        /// <summary>
        /// Called once before iteration starts.
        /// </summary>
        /// <param name="timestep">The simulation timestep</param>
        public override void PrepareForIteration(float timestep)
        {
            Vector3.Transform(ref localAnchor1, ref body1.orientation, out r1);
            Vector3.Transform(ref localAnchor2, ref body2.orientation, out r2);

            Vector3 p1, p2, dp;
            Vector3.Add(ref body1.position, ref r1, out p1);
            Vector3.Add(ref body2.position, ref r2, out p2);

            Vector3.Subtract(ref p2, ref p1, out dp);

            Vector3 l = Vector3.Transform(lineNormal, body1.orientation);
            l.Normalize();

			Vector3 t = Vector3.Cross((p1 - p2), l);
            if(t.LengthSquared != 0.0f) t.Normalize();
			t = Vector3.Cross(t, l);

            jacobian[0] = t;                      // linearVel Body1
			jacobian[1] = Vector3.Cross((r1 + p2 - p1), t);     // angularVel Body1
            jacobian[2] = -1.0f * t;              // linearVel Body2
			jacobian[3] = -1.0f * Vector3.Cross(r2, t);         // angularVel Body2

            effectiveMass = body1.inverseMass + body2.inverseMass
			    + Vector3.Dot(Vector3.Transform(jacobian[1], body1.invInertiaWorld),jacobian[1])
			    + Vector3.Dot(Vector3.Transform(jacobian[3], body2.invInertiaWorld), jacobian[3]);

            softnessOverDt = softness / timestep;
            effectiveMass += softnessOverDt;

            if(effectiveMass != 0) effectiveMass = 1.0f / effectiveMass;

			bias = - (Vector3.Cross(l, (p2-p1))).Length * biasFactor * (1.0f / timestep);

            if (!body1.isStatic)
            {
                body1.linearVelocity += body1.inverseMass * accumulatedImpulse * jacobian[0];
                body1.angularVelocity += Vector3.Transform(accumulatedImpulse * jacobian[1], body1.invInertiaWorld);
            }

            if (!body2.isStatic)
            {
                body2.linearVelocity += body2.inverseMass * accumulatedImpulse * jacobian[2];
                body2.angularVelocity += Vector3.Transform(accumulatedImpulse * jacobian[3], body2.invInertiaWorld);
            }
        }

        /// <summary>
        /// Iteratively solve this constraint.
        /// </summary>
        public override void Iterate()
        {
            float jv =
				Vector3.Dot(body1.linearVelocity, jacobian[0]) +
				Vector3.Dot(body1.angularVelocity, jacobian[1]) +
				Vector3.Dot(body2.linearVelocity, jacobian[2]) +
				Vector3.Dot(body2.angularVelocity, jacobian[3]);

            float softnessScalar = accumulatedImpulse * softnessOverDt;

            float lambda = -effectiveMass * (jv + bias + softnessScalar);

            accumulatedImpulse += lambda;

            if (!body1.isStatic)
            {
                body1.linearVelocity += body1.inverseMass * lambda * jacobian[0];
                body1.angularVelocity += Vector3.Transform(lambda * jacobian[1], body1.invInertiaWorld);
            }

            if (!body2.isStatic)
            {
                body2.linearVelocity += body2.inverseMass * lambda * jacobian[2];
                body2.angularVelocity += Vector3.Transform(lambda * jacobian[3], body2.invInertiaWorld);
            }
        }

        public override void DebugDraw(IDebugDrawer drawer)
        {
            drawer.DrawLine(body1.position + r1,
                body1.position + r1 + Vector3.Transform(lineNormal, body1.orientation) * 100.0f);
        }

    }
}
