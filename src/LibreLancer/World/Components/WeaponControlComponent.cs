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
    // For objects that shoot
    public class WeaponControlComponent : GameComponent
    {
        public Vector3 AimPoint = Vector3.Zero;
        public bool Enabled { get; set; } = true;
        private double DryFireTimer { get; set; }
        public WeaponComponent[]? NetOrderWeapons;
        private readonly Dictionary<WeaponComponent, bool> mouseEnabled = [];

        public WeaponControlComponent(GameObject parent) : base(parent)
        {
        }

        public override void Update(double time, GameWorld world)
        {
            DryFireTimer += time;

            if (AimPoint == Vector3.Zero)
            {
                return;
            }

            world.DrawDebug(AimPoint);

            foreach (var wp in Parent?.GetChildComponents<WeaponComponent>()!)
            {
                wp?.AimTowards(AimPoint, time);
            }
        }

        public void UpdateNetWeapons()
        {
            NetOrderWeapons = Parent.GetChildComponents<WeaponComponent>()!
                .OrderBy(x => x.Parent.Attachment?.Name.ToLowerInvariant())
                .ToArray();
            GetWeapons();
        }

        private static bool IsDefaultMouseEnabled(WeaponComponent weapon) => weapon is GunComponent;

        public bool ToggleMouseEnabled(int index)
        {
            var weapon = GetWeapons().Skip(index).FirstOrDefault();
            if (weapon == null)
                return false;
            mouseEnabled[weapon] = !mouseEnabled[weapon];
            return mouseEnabled[weapon];
        }

        private bool IsMouseEnabled(WeaponComponent weapon) =>
            mouseEnabled.TryGetValue(weapon, out var enabled) ? enabled : IsDefaultMouseEnabled(weapon);

        private WeaponComponent[] GetWeapons()
        {
            var weapons = Parent.GetChildComponents<WeaponComponent>().ToArray();
            foreach (var weapon in weapons)
                mouseEnabled.TryAdd(weapon, IsDefaultMouseEnabled(weapon));
            foreach (var weapon in mouseEnabled.Keys.Except(weapons).ToArray())
                mouseEnabled.Remove(weapon);
            return weapons;
        }

        public override void Register(GameWorld world)
        {
            UpdateNetWeapons();
        }

        public void SetRotations(GunOrient[] orients)
        {
            for (var i = 0; i < orients.Length && i < NetOrderWeapons!.Length; i++)
            {
                NetOrderWeapons[i].RotateTowards(orients[i].AngleRot, orients[i].AnglePitch);
            }
        }

        public GunOrient[]? GetRotations()
        {
            return NetOrderWeapons?.Select(x => new GunOrient()
            {
                AngleRot = x.Angles.X,
                AnglePitch = x.Angles.Y
            }).ToArray();
        }

        public float GetAverageGunSpeed()
        {
            float accum = 0;
            var count = 0;

            foreach (var wp in Parent!.GetChildComponents<GunComponent>())
            {
                accum += wp.Object.Def.MuzzleVelocity;
                count++;
            }

            return accum / count;
        }

        public float GetGunMaxRange()
        {
            return Parent.GetChildComponents<GunComponent>().Select(wp => wp.MaxRange).Prepend(0).Max();
        }

        public float GetMissileMaxRange()
        {
            return Parent!.GetChildComponents<MissileLauncherComponent>().Select(wp => wp.MaxRange).Prepend(0).Max();
        }

        public bool CanFireWeapons(GameWorld world)
        {
            if (Enabled && (Parent!.Flags & GameObjectFlags.Cloaked) != GameObjectFlags.Cloaked &&
                (!Parent.TryGetComponent<ShipPhysicsComponent>(out var flight) ||
                 (flight.EngineState != EngineStates.Cruise && flight.EngineState != EngineStates.CruiseCharging)))
            {
                return true;
            }

            PlayDryFireSound(world);
            return false;

        }

        private void PlayDryFireSound(GameWorld world)
        {
            if (DryFireTimer < 1.0)
                return;

            DryFireTimer = 0.0;
            GetSoundManager(world)?.PlayOneShot("fire_dry");
        }

        public void FireIndex(int index, GameWorld world)
        {
            if (!CanFireWeapons(world)) return;
            var wp = Parent?.GetChildComponents<WeaponComponent>()
                .Skip(index).FirstOrDefault();
            wp?.Fire(AimPoint, world);
        }

        public void FireMissiles(GameWorld world)
        {
            if (!CanFireWeapons(world)) return;

            foreach (var wp in Parent?.GetChildComponents<MissileLauncherComponent>()!)
            {
                wp?.Fire(AimPoint, world);
            }
        }

        public void FireGuns(GameWorld world)
        {
            if (!CanFireWeapons(world)) return;

            foreach (var wp in Parent?.GetChildComponents<GunComponent>()!)
            {
                wp?.Fire(AimPoint, world);
            }
        }

        public void FireAll(GameWorld world)
        {
            if (!CanFireWeapons(world)) return;

            foreach (var wp in GetWeapons())
            {
                if (IsMouseEnabled(wp))
                    wp.Fire(AimPoint, world);
            }
        }

        public IEnumerable<UiEquippedWeapon> GetUiElements()
        {
            return from wp in GetWeapons()
                select new UiEquippedWeapon(IsMouseEnabled(wp), wp.IdsName);
        }
    }
}
