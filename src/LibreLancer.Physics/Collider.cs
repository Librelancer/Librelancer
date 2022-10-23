// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using BulletSharp;
using BM = BulletSharp.Math;
namespace LibreLancer.Physics
{
    public abstract class Collider : IDisposable
    {
        internal abstract CollisionShape BtShape { get; }

        internal bool isDisposed = false;
        public virtual void Dispose()
        {
            if(BtShape != null) {
                isDisposed = true;
                BtShape.Dispose();
            }
        }
        public virtual float Radius {
            get {
                //This seems to return incorrect values. Even for spheres :/
                if (isDisposed) throw new ObjectDisposedException("Collider");
                BtShape.GetBoundingSphere(out _, out float r);
                return r;
            }
        }

    }
}
