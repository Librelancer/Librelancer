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
    public class SphereCollider(float radius) : Collider
    {
        public override float Radius { get; } = radius;

        internal override void Create(Simulation simulation, BufferPool bufferPool)
        {
            base.Create(simulation, bufferPool);
            if (!Handle.Exists)
            {
                Handle = simulation.Shapes.Add(new Sphere(Radius));
            }
        }

        internal override Symmetric3x3 CalculateInverseInertia(float mass)
        {
            return new Sphere(Radius).ComputeInertia(mass).InverseInertiaTensor;
        }
    }
}
