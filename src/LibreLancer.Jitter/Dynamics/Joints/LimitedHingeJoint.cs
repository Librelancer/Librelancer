using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Jitter.Dynamics.Joints;
using LibreLancer.Jitter.Dynamics.Constraints;

namespace LibreLancer.Jitter.Dynamics.Joints
{

    /// <summary>
    /// Limited hinge joint.
    /// </summary>
    public class LimitedHingeJoint : Joint
    {


        private PointOnPoint[] worldPointConstraint;
        private PointPointDistance distance;

        public PointOnPoint PointConstraint1 { get { return worldPointConstraint[0]; } }
        public PointOnPoint PointConstraint2 { get { return worldPointConstraint[1]; } }

        public PointPointDistance DistanceConstraint { get { return distance; } }


        /// <summary>
        /// Initializes a new instance of the HingeJoint class.
        /// </summary>
        /// <param name="world">The world class where the constraints get added to.</param>
        /// <param name="body1">The first body connected to the second one.</param>
        /// <param name="body2">The second body connected to the first one.</param>
        /// <param name="position">The position in world space where both bodies get connected.</param>
        /// <param name="hingeAxis">The axis if the hinge.</param>
        public LimitedHingeJoint(World world, RigidBody body1, RigidBody body2, Vector3 position, Vector3 hingeAxis,
            float hingeFwdAngle, float hingeBckAngle)
            : base(world)
        {
            // Create the hinge first, two point constraints

            worldPointConstraint = new PointOnPoint[2];

            hingeAxis *= 0.5f;

            Vector3 pos1 = position; Vector3.Add(ref pos1, ref hingeAxis, out pos1);
            Vector3 pos2 = position; Vector3.Subtract(ref pos2, ref hingeAxis, out pos2);

            worldPointConstraint[0] = new PointOnPoint(body1, body2, pos1);
            worldPointConstraint[1] = new PointOnPoint(body1, body2, pos2);


            // Now the limit, one max distance constraint

            hingeAxis.Normalize();

            // choose a direction that is perpendicular to the hinge
            Vector3 perpDir = Vector3.Up;

            if (Vector3.Dot(perpDir, hingeAxis) > 0.1f) perpDir = Vector3.Right;

            // now make it perpendicular to the hinge
            Vector3 sideAxis = Vector3.Cross(hingeAxis, perpDir);
            perpDir = Vector3.Cross(sideAxis, hingeAxis);
            perpDir.Normalize();

            // the length of the "arm" TODO take this as a parameter? what's
            // the effect of changing it?
            float len = 10.0f * 3;

            // Choose a position using that dir. this will be the anchor point
            // for body 0. relative to hinge
            Vector3 hingeRelAnchorPos0 = perpDir * len;


            // anchor point for body 2 is chosen to be in the middle of the
            // angle range.  relative to hinge
            float angleToMiddle = 0.5f * (hingeFwdAngle - hingeBckAngle);
            Vector3 hingeRelAnchorPos1 = Vector3.Transform(hingeRelAnchorPos0, Matrix3.CreateFromAxisAngle(hingeAxis, -angleToMiddle / 360.0f * 2.0f * JMath.Pi));

            // work out the "string" length
            float hingeHalfAngle = 0.5f * (hingeFwdAngle + hingeBckAngle);
            float allowedDistance = len * 2.0f * (float)System.Math.Sin(hingeHalfAngle * 0.5f / 360.0f * 2.0f * JMath.Pi);

            Vector3 hingePos = body1.Position;
            Vector3 relPos0c = hingePos + hingeRelAnchorPos0;
            Vector3 relPos1c = hingePos + hingeRelAnchorPos1;

            distance = new PointPointDistance(body1, body2, relPos0c, relPos1c);
            distance.Distance = allowedDistance;
            distance.Behavior = PointPointDistance.DistanceBehavior.LimitMaximumDistance;

        }


        /// <summary>
        /// Adds the internal constraints of this joint to the world class.
        /// </summary>
        public override void Activate()
        {
            World.AddConstraint(worldPointConstraint[0]);
            World.AddConstraint(worldPointConstraint[1]);
            World.AddConstraint(distance);
        }

        /// <summary>
        /// Removes the internal constraints of this joint from the world class.
        /// </summary>
        public override void Deactivate()
        {
            World.RemoveConstraint(worldPointConstraint[0]);
            World.RemoveConstraint(worldPointConstraint[1]);
            World.RemoveConstraint(distance);
        }
    }
}
