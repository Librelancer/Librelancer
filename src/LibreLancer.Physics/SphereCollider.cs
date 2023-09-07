// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

namespace LibreLancer.Physics
{
    public class SphereCollider : Collider
    {
        public SphereCollider(float radius)
        {
            Radius = radius;
        }

        public override float Radius { get; }
        internal override void Create(Simulation sim, BufferPool pool)
        {
            base.Create(sim, pool);
            if (!Handle.Exists){
                Handle = sim.Shapes.Add(new Sphere(Radius));
            }
        }

        public override Symmetric3x3 CalculateInverseInertia(float mass)
        {
            return new Sphere(Radius).ComputeInertia(mass).InverseInertiaTensor;
        }
    }
}
