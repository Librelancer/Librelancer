// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

namespace LibreLancer.Physics
{
    public abstract class Collider : IDisposable
    {
        protected Simulation sim;
        protected BufferPool pool;
        private bool isDisposed = false;
        public TypedIndex Handle { get; protected set; }

        public void Dispose()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Collider has already been disposed!");
            }
            sim.Shapes.RemoveAndDispose(Handle, pool);
            Handle = new TypedIndex();
            isDisposed = true;
        }
        public abstract float Radius { get; }

        internal virtual void Create(Simulation sim, BufferPool pool)
        {
            this.sim = sim;
            this.pool = pool;
        }

        internal virtual void Draw(Matrix4x4 transform, IDebugRenderer renderer)
        {
        }

        public abstract Symmetric3x3 CalculateInverseInertia(float mass);
    }
}
