// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Interface;
using LibreLancer.Net.Protocol;
using LibreLancer.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Physics;
using LibreLancer.Server.Components;

namespace LibreLancer.World.Components
{
    //For objects that shoot
    public class WeaponControlComponent : GameComponent
    {
        public Vector3 AimPoint = Vector3.Zero;
        public bool Enabled { get; set; } = true;
        private double DryFireTimer { get; set; }
        public WeaponControlComponent(GameObject parent) : base(parent)
        {
        }

        public override void Update(double time)
        {
            DryFireTimer += time;

            if (AimPoint != Vector3.Zero)
            {
                Parent.World.DrawDebug(AimPoint);
                foreach (var wp in Parent.GetChildComponents<WeaponComponent>())
                {
                    wp.AimTowards(AimPoint, time);
                }
            }
        }

        public WeaponComponent[] NetOrderWeapons;

        public void UpdateNetWeapons()
        {
            NetOrderWeapons = Parent.GetChildComponents<WeaponComponent>()
                .OrderBy(x => x.Parent.Attachment.Name.ToLowerInvariant())
                .ToArray();
        }

        public override void Register(PhysicsWorld physics)
        {
            UpdateNetWeapons();
        }

        public void SetRotations(GunOrient[] orients)
        {
            for (int i = 0; i < orients.Length && i < NetOrderWeapons.Length; i++) {
                NetOrderWeapons[i].RotateTowards(orients[i].AngleRot, orients[i].AnglePitch);
            }
        }

        public GunOrient[] GetRotations()
        {
            return NetOrderWeapons.Select(x => new GunOrient()
            {
                AngleRot = x.Angles.X,
                AnglePitch = x.Angles.Y
            }).ToArray();
        }

        public float GetAverageGunSpeed()
        {
            float accum = 0;
            int count = 0;
            foreach (var wp in Parent.GetChildComponents<GunComponent>())
            {
                accum += wp.Object.Def.MuzzleVelocity;
                count++;
            }
            return accum / count;
        }
        public float GetGunMaxRange()
        {
            float range = 0;
            foreach (var wp in Parent.GetChildComponents<GunComponent>())
            {
                var r = wp.MaxRange;
                if (r > range) range = r;
            }
            return range;
        }

        public float GetMissileMaxRange()
        {
            float range = 0;
            foreach (var wp in Parent.GetChildComponents<MissileLauncherComponent>())
            {
                var r = wp.MaxRange;
                if (r > range) range = r;
            }
            return range;
        }

        public bool CanFireWeapons()
        {
            if (!Enabled ||
                (Parent.Flags & GameObjectFlags.Cloaked) == GameObjectFlags.Cloaked ||
                (Parent.TryGetComponent<ShipPhysicsComponent>(out var flight) &&
                (flight.EngineState == EngineStates.Cruise || flight.EngineState == EngineStates.CruiseCharging)))
            {
                PlayDryFireSound();
                return false;
            }
            return true;
        }

        private void PlayDryFireSound()
        {
            if (DryFireTimer < 1.0)
                return;

            DryFireTimer = 0.0;
            SoundManager snd = Parent.World.Renderer?.Game.GetService<SoundManager>();
            snd?.PlayOneShot("fire_dry");
        }

        public void FireIndex(int index)
        {
            if (!CanFireWeapons()) return;
            var wp = Parent.GetChildComponents<WeaponComponent>()
                .Skip(index).FirstOrDefault();
            wp?.Fire(AimPoint);
        }

        public void FireMissiles()
        {
            if (!CanFireWeapons()) return;
            foreach (var wp in Parent.GetChildComponents<MissileLauncherComponent>())
            {
                wp.Fire(AimPoint);
            }
        }

        public void FireGuns()
        {
            if (!CanFireWeapons()) return;
            foreach(var wp in Parent.GetChildComponents<GunComponent>())
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
