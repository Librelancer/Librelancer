// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
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

        public bool Active => !Static && RigidBody.ActivationState != ActivationState.IslandSleeping;

        public Matrix4x4 Transform { get; private set; }

        public Vector3 Position { get; private set; }

        public void SetTransform(Matrix4x4 transform)
        {
            Transform = transform;
            RigidBody.WorldTransform = transform.Cast();
            Position = Vector3.Transform(Vector3.Zero, Transform);
        }

        public Vector3 AngularVelocity
        {
            get {
                var ang = RigidBody.AngularVelocity.Cast();
                if (ang.LengthSquared() < float.Epsilon) return Vector3.Zero;
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
            return Vector3.Transform(src, RigidBody.WorldTransform.Cast().ClearTranslation());
        }

        public void SetDamping(float linearDamping, float angularDamping)
        {
            RigidBody.SetDamping(linearDamping,angularDamping);
        }

        public void AddForce(Vector3 force)
        {
            if (force.LengthSquared() > float.Epsilon)
            {
                RigidBody.Activate(true);
                RigidBody.ApplyForce(force.Cast(), BM.Vector3.Zero);
            }
        }
        public void Impulse(Vector3 force)
        {
            if(force.LengthSquared() > float.Epsilon)
            {
                RigidBody.Activate(true);
                RigidBody.ApplyImpulse(force.Cast(), BM.Vector3.Zero);
            }
        }
        public void AddTorque(Vector3 torque)
        {
            if (torque.LengthSquared() > float.Epsilon)
            {
                RigidBody.Activate(true);
                RigidBody.ApplyTorque(torque.Cast());
            }
        }

        internal void UpdateProperties()
        {
            Transform = RigidBody.WorldTransform.Cast();
            Position = Vector3.Transform(Vector3.Zero, Transform);
        }
    }
}
