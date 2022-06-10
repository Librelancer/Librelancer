// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
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

        public override void Update(double time)
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

        public void SetRotations(GunOrient[] orients)
        {
            foreach (var wp in Parent.GetChildComponents<WeaponComponent>())
            {
                var hp = CrcTool.FLModelCrc(wp.Parent.Attachment.Name);
                foreach (var o in orients) {
                    if (o.Hardpoint == hp)
                    {
                        wp.RotateTowards(o.AngleRot, o.AnglePitch);
                        break;
                    }
                }
            }
        }

        public GunOrient[] GetRotations()
        {
            return Parent.GetChildComponents<WeaponComponent>().Select(x => new GunOrient()
            {
                Hardpoint = CrcTool.FLModelCrc(x.Parent.Attachment.Name),
                AngleRot = x.Angles.X,
                AnglePitch = x.Angles.Y
            }).ToArray();
        }
        

        public float GetMaxRange()
        {
            float range = 0;
            foreach (var wp in Parent.GetChildComponents<WeaponComponent>())
            {
                var r = wp.Object.Munition.Def.Lifetime * wp.Object.Def.MuzzleVelocity;
                if (r > range) range = r;
            }
            return range;
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
