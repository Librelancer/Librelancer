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
using LibreLancer.Jitter.Dynamics.Constraints;
#endregion

namespace LibreLancer.Jitter.Dynamics.Joints
{
    public class PrismaticJoint : Joint
    {
        // form prismatic joint
        FixedAngle fixedAngle;
        PointOnLine pointOnLine;

        PointPointDistance minDistance = null;
        PointPointDistance maxDistance = null;

        public PointPointDistance MaximumDistanceConstraint { get { return maxDistance; } }
        public PointPointDistance MinimumDistanceConstraint { get { return minDistance; } }

        public FixedAngle FixedAngleConstraint { get { return fixedAngle; } }
        public PointOnLine PointOnLineConstraint { get { return pointOnLine; } }

        public PrismaticJoint(World world, RigidBody body1, RigidBody body2)
            : base(world)
        {
            fixedAngle = new FixedAngle(body1, body2);
            pointOnLine = new PointOnLine(body1, body2, body1.position, body2.position);
        }

        public PrismaticJoint(World world, RigidBody body1, RigidBody body2,float minimumDistance, float maximumDistance)
            : base(world)
        {
            fixedAngle = new FixedAngle(body1, body2);
            pointOnLine = new PointOnLine(body1, body2, body1.position, body2.position);

            minDistance = new PointPointDistance(body1, body2, body1.position, body2.position);
            minDistance.Behavior = PointPointDistance.DistanceBehavior.LimitMinimumDistance;
            minDistance.Distance = minimumDistance;

            maxDistance = new PointPointDistance(body1, body2, body1.position, body2.position);
            maxDistance.Behavior = PointPointDistance.DistanceBehavior.LimitMaximumDistance;
            maxDistance.Distance = maximumDistance;
        }


        public PrismaticJoint(World world, RigidBody body1, RigidBody body2, Vector3 pointOnBody1,Vector3 pointOnBody2)
            : base(world)
        {
            fixedAngle = new FixedAngle(body1, body2);
            pointOnLine = new PointOnLine(body1, body2, pointOnBody1, pointOnBody2);
        }


        public PrismaticJoint(World world, RigidBody body1, RigidBody body2, Vector3 pointOnBody1, Vector3 pointOnBody2, float maximumDistance, float minimumDistance)
            : base(world)
        {
            fixedAngle = new FixedAngle(body1, body2);
            pointOnLine = new PointOnLine(body1, body2, pointOnBody1, pointOnBody2);
        }

        public override void Activate()
        {
            if (maxDistance != null) World.AddConstraint(maxDistance);
            if (minDistance != null) World.AddConstraint(minDistance);

            World.AddConstraint(fixedAngle);
            World.AddConstraint(pointOnLine);
        }

        public override void Deactivate()
        {
            if (maxDistance != null) World.RemoveConstraint(maxDistance);
            if (minDistance != null) World.RemoveConstraint(minDistance);

            World.RemoveConstraint(fixedAngle);
            World.RemoveConstraint(pointOnLine);
        }
    }
}
