// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Interface;

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
                foreach (var o in orients) {
                    if (o.Hardpoint.Equals(wp.Parent.Attachment.Name,StringComparison.OrdinalIgnoreCase))
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
                Hardpoint = x.Parent.Attachment.Name,
                AngleRot = x.Angles.X,
                AnglePitch = x.Angles.Y
            }).ToArray();
        }

        public float GetMaxRange()
        {
            float range = 0;
            foreach (var wp in Parent.GetChildComponents<WeaponComponent>())
            {
                var r = wp.MaxRange;
                if (r > range) range = r;
            }
            return range;
        }

        bool CanFireWeapons()
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var flight) &&
                (flight.EngineState == EngineStates.Cruise || flight.EngineState == EngineStates.CruiseCharging))
                return false;
            return true;
        }

        public void FireMissiles()
        {
            if (!CanFireWeapons()) return;
            foreach (var wp in Parent.GetChildComponents<MissileLauncherComponent>())
            {
                wp.Fire(AimPoint);
            }
        }
        
        public void FireAll()
        {
            if (!CanFireWeapons()) return;
            foreach(var wp in Parent.GetChildComponents<WeaponComponent>())
            {
                wp.Fire(AimPoint);
            }
        }

        public IEnumerable<UiEquippedWeapon> GetUiElements()
        {
            foreach (var wp in Parent.GetChildComponents<WeaponComponent>())
            {
                yield return new UiEquippedWeapon(true, wp.IdsName);
            }
        }
    }
}
