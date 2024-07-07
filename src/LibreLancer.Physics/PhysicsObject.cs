// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Physics
{
    public abstract class PhysicsObject : IDisposable
    {
        public object Tag;
        internal int Id;

        protected PhysicsObject(int id)
        {
            Id = id;
        }

        public abstract bool Static { get; }
        public abstract bool Active { get; }
        public Collider Collider { get; internal set;  }

        public abstract void SetOrientation(Quaternion orientation);

        public abstract Vector3 Position { get; protected set; }

        public abstract Quaternion Orientation { get; protected set; }

        public abstract void SetTransform(Transform3D transform);

        public abstract Vector3 AngularVelocity { get; set; }

        public abstract Vector3 LinearVelocity { get; set; }

        public abstract bool Collidable { get; set; }

        public abstract BoundingBox GetBoundingBox();

        public abstract Vector3 RotateVector(Vector3 src);

        public abstract void SetDamping(float linearDamping, float angularDamping);

        public abstract void AddForce(Vector3 force);

        public abstract void Activate();

        public abstract void Impulse(Vector3 force);

        public abstract void AddTorque(Vector3 torque);

        /// <summary>
        /// Runs a step without collision detection or proper damping.
        /// Useful for player reconciliation only
        /// </summary>
        /// <param name="timestep">Should be 1/60.0 normally</param>
        public abstract void PredictionStep(float timestep);

        internal abstract void UpdateProperties();

        public abstract void Dispose();
    }
}
