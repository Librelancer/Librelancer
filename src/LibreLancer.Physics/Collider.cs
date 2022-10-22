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
        public virtual void Dispose()
        {
            if(BtShape != null) {
                BtShape.Dispose();
            }
        }
        public virtual float Radius {
            get {
                //This seems to return incorrect values. Even for spheres :/
                BtShape.GetBoundingSphere(out _, out float r);
                return r;
            }
        }

    }
}
