// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using BulletSharp;
namespace LibreLancer.Physics
{
    public class SphereCollider : Collider
    {
        SphereShape btSphere;
        internal override CollisionShape BtShape
        {
            get
            {
                return btSphere;
            }
        }
        public SphereCollider(float radius)
        {
            btSphere = new SphereShape(radius);
        }
    }
}
