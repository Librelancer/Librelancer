/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using BulletSharp;
using BM = BulletSharp.Math;
namespace LibreLancer.Physics
{
    public class PhysicsObject
    {
        public object Tag;
        public bool Static { get; internal set; }
        internal RigidBody RigidBody { get; private set; }
        public Collider Collider { get; internal set;  }
        internal PhysicsObject(RigidBody rb, Collider col)
        {
            RigidBody = rb;
            Collider = col;
        }

        public Matrix4 Transform
        {
            get { return RigidBody.WorldTransform.Cast();  }
        }

        public Vector3 Position
        {
            get; private set;
        }

        public void SetTransform(Matrix4 transform)
        {
            RigidBody.WorldTransform = transform.Cast();
            Position = VectorMath.Transform(Vector3.Zero, Transform);
        }

        public Vector3 AngularVelocity
        {
            get {
                var ang = RigidBody.AngularVelocity.Cast();
                if (ang.LengthSquared < float.Epsilon) return Vector3.Zero;
                return ang;
            } set {
                RigidBody.AngularVelocity = value.Cast();
            }
        }

        public Vector3 LinearVelocity
        {
            get {
                return RigidBody.LinearVelocity.Cast();
            }
            set {
                RigidBody.LinearVelocity = value.Cast();
            }
        }

        public BoundingBox GetBoundingBox()
        {
            BM.Vector3 min, max;
            Collider.BtShape.GetAabb(RigidBody.WorldTransform, out min, out max);
            return new BoundingBox(min.Cast(), max.Cast());
        }

        public Vector3 RotateVector(Vector3 src)
        {
            return VectorMath.Transform(src, RigidBody.WorldTransform.Cast().ClearTranslation());
        }

        public void SetDamping(float linearDamping, float angularDamping)
        {
            RigidBody.SetDamping(linearDamping,angularDamping);
        }

        public void AddForce(Vector3 force)
        {
            if (force.LengthSquared > float.Epsilon)
            {
                RigidBody.Activate(true);
                RigidBody.ApplyForce(force.Cast(), BM.Vector3.Zero);
            }
        }

        public void AddTorque(Vector3 torque)
        {
            if (torque.LengthSquared > float.Epsilon)
            {
                RigidBody.Activate(true);
                RigidBody.ApplyTorque(torque.Cast());
            }
        }

        internal void UpdateProperties()
        {
            Position = VectorMath.Transform(Vector3.Zero, Transform);
        }
    }
}
