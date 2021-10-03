// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
namespace LibreLancer
{
    //For objects that shoot
    public class WeaponControlComponent : GameComponent
    {
        public Vector3 AimPoint = Vector3.Zero;
        public WeaponControlComponent(GameObject parent) : base(parent)
        {
        }

        public override void FixedUpdate(double time)
        {
            if (AimPoint != Vector3.Zero)
            {
                Parent.World.DrawDebug(AimPoint);
                foreach (var wp in Parent.GetChildComponents<WeaponComponent>())
                {
                    wp.AimTowards(AimPoint, time);
                }
            }
        }

        public void FireAll()
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var flight) &&
                (flight.EngineState == EngineStates.Cruise || flight.EngineState == EngineStates.CruiseCharging))
                return;
            foreach(var wp in Parent.GetChildComponents<WeaponComponent>())
            {
                wp.Fire(AimPoint);
            }
        }

    }
}
