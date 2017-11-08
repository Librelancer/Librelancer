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
    // C_1 = R1_x - R2_x
    // C_2 = ...
    // C_3 = ...
    //
    // Derivative:
    //
    // dC_1/dt = w1_x - w2_x
    // dC_2/dt = ...
    // dC_3/dt = ...
    //
    // Jacobian:
    // 
    // dC/dt = J*v+b
    //
    // v = (v1x v1y v1z w1x w1y w1z v2x v2y v2z w2x w2y w2z)^(T) 
    //
    //     v1x v1y v1z w1x w1y w1z v2x v2y v2z w2x w2y w2z
    //     -------------------------------------------------
    // J = 0   0   0   1   0    0   0   0   0   -1   0   0   <- dC_1/dt
    //     0   0   0   0   1    0   0   0   0    0  -1   0   <- ...  
    //     0   0   0   0   0    1   0   0   0    0   0  -1   <- ...
    //
    // Effective Mass:
    //
    // 1/m_eff = [J^T * M^-1 * J] = I1^(-1) + I2^(-1)
    #endregion

    /// <summary>
    /// The AngleConstraint constraints two bodies to always have the same relative
    /// orientation to each other. Combine the AngleConstraint with a PointOnLine
    /// Constraint to get a prismatic joint.
    /// </summary>
    public class FixedAngle : Constraint
    {

        private float biasFactor = 0.05f;
        private float softness = 0.0f;

        private Vector3 accumulatedImpulse;

        private Matrix3 initialOrientation1, initialOrientation2;

        /// <summary>
        /// Constraints two bodies to always have the same relative
        /// orientation to each other. Combine the AngleConstraint with a PointOnLine
        /// Constraint to get a prismatic joint.
        /// </summary>
        public FixedAngle(RigidBody body1, RigidBody body2) : base(body1, body2)
        {
            initialOrientation1 = body1.orientation;
            initialOrientation2 = body2.orientation;

            //orientationDifference = body1.orientation * body2.invOrientation;
            //orientationDifference = JMatrix.Transpose(orientationDifference);
        }

        public Vector3 AppliedImpulse { get { return accumulatedImpulse; } }

        public Matrix3 InitialOrientationBody1 { get { return initialOrientation1; } set { initialOrientation1 = value; } }
        public Matrix3 InitialOrientationBody2 { get { return initialOrientation2; } set { initialOrientation2 = value; } }

        /// <summary>
        /// Defines how big the applied impulses can get.
        /// </summary>
        public float Softness { get { return softness; } set { softness = value; } }

        /// <summary>
        /// Defines how big the applied impulses can get which correct errors.
        /// </summary>
        public float BiasFactor { get { return biasFactor; } set { biasFactor = value; } }

        Matrix3 effectiveMass;
        Vector3 bias;
        float softnessOverDt;
        
        /// <summary>
        /// Called once before iteration starts.
        /// </summary>
        /// <param name="timestep">The 5simulation timestep</param>
        public override void PrepareForIteration(float timestep)
        {
			effectiveMass = Matrix3.Add(body1.invInertiaWorld, body2.invInertiaWorld);

            softnessOverDt = softness / timestep;

            effectiveMass.M11 += softnessOverDt;
            effectiveMass.M22 += softnessOverDt;
            effectiveMass.M33 += softnessOverDt;

            Matrix3.Invert(ref effectiveMass, out effectiveMass);

            Matrix3 orientationDifference;
            Matrix3.Mult(ref initialOrientation1, ref initialOrientation2, out orientationDifference);
            Matrix3.Transpose(ref orientationDifference, out orientationDifference);

            Matrix3 q = orientationDifference * body2.invOrientation * body1.orientation;
            Vector3 axis;

            float x = q.M32 - q.M23;
            float y = q.M13 - q.M31;
            float z = q.M21 - q.M12;

            float r = JMath.Sqrt(x * x + y * y + z * z);
            float t = q.M11 + q.M22 + q.M33;

            float angle = (float)Math.Atan2(r, t - 1);
            axis = new Vector3(x, y, z) * angle;

            if (r != 0.0f) axis = axis * (1.0f / r);

            bias = axis * biasFactor * (-1.0f / timestep);

            // Apply previous frame solution as initial guess for satisfying the constraint.
            if (!body1.IsStatic) body1.angularVelocity += Vector3.Transform(accumulatedImpulse, body1.invInertiaWorld);
            if (!body2.IsStatic) body2.angularVelocity += Vector3.Transform(-1.0f * accumulatedImpulse, body2.invInertiaWorld);
        }

        /// <summary>
        /// Iteratively solve this constraint.
        /// </summary>
        public override void Iterate()
        {
            Vector3 jv = body1.angularVelocity - body2.angularVelocity;

            Vector3 softnessVector = accumulatedImpulse * softnessOverDt;

            Vector3 lambda = -1.0f * Vector3.Transform(jv+bias+softnessVector, effectiveMass);

            accumulatedImpulse += lambda;

            if(!body1.IsStatic) body1.angularVelocity += Vector3.Transform(lambda, body1.invInertiaWorld);
            if(!body2.IsStatic) body2.angularVelocity += Vector3.Transform(-1.0f * lambda, body2.invInertiaWorld);
        }

    }
}
