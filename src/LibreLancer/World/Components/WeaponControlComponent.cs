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
using LibreLancer.World.Equipments;

namespace LibreLancer.World.Components
{
    // For objects that shoot
    public class WeaponControlComponent : GameComponent
    {
        public Vector3 AimPoint = Vector3.Zero;
        public bool Enabled { get; set; } = true;
        private double DryFireTimer { get; set; }
        public AbstractWeapon[] AllWeapons => weapons;

        private AbstractWeapon[] weapons = [];
        private Dictionary<AbstractWeapon, bool> activatedWeapons = [];

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

            foreach (var wp in weapons)
            {
                wp.AimTowards(AimPoint, time);
            }

        }

        public override void ResolveReferences()
        {
            weapons = Parent.EquipmentOfType<AbstractWeapon>()
                .OrderBy(x => x switch
                {
                    MineDropper => 1,
                    CountermeasureLauncher => 2,
                    _ => 0
                })
                .ThenBy(x => x.Attachment?.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var existing = activatedWeapons;
            activatedWeapons = [];
            foreach (var weapon in weapons)
            {
                if (existing.TryGetValue(weapon, out var act))
                    activatedWeapons[weapon] = act;
                else
                    activatedWeapons[weapon] = IsDefaultWeaponEnabled(weapon);
            }
        }


        private static bool IsDefaultWeaponEnabled(AbstractWeapon weapon) => weapon is Gun;

        public bool ToggleWeaponEnabled(int index)
        {
            var weapon = weapons[index];
            activatedWeapons[weapon] = !activatedWeapons[weapon];
            return activatedWeapons[weapon];
        }

        private bool IsWeaponEnabled(AbstractWeapon weapon) =>
            activatedWeapons.TryGetValue(weapon, out var enabled) ? enabled : IsDefaultWeaponEnabled(weapon);


        public void SetRotations(GunOrient[] orients)
        {
            for (var i = 0; i < orients.Length && i < weapons.Length; i++)
            {
                weapons[i].RotateTowards(orients[i].AngleRot, orients[i].AnglePitch);
            }
        }

        public GunOrient[]? GetRotations()
        {
            return weapons.Select(x => new GunOrient()
            {
                AngleRot = x.Angles.X,
                AnglePitch = x.Angles.Y
            }).ToArray();
        }

        public float GetAverageGunSpeed()
        {
            float accum = 0;
            var count = 0;

            foreach (var wp in weapons.OfType<Gun>())
            {
                accum += wp.GunEquipment.Def.MuzzleVelocity;
                count++;
            }

            return count > 0 ? accum / count : 0;
        }

        public float GetGunMaxRange()
        {
            return weapons.OfType<Gun>().Select(wp => wp.MaxRange).Prepend(0).Max();
        }

        public float GetMissileMaxRange()
        {
            return weapons.OfType<MissileLauncher>().Select(wp => wp.MaxRange).Prepend(0).Max();
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
            weapons[index].Fire(AimPoint, world);
        }

        public void FireMissiles(GameWorld world)
            => FireWeapons<MissileLauncher>(world, AimPoint);

        public void FireCountermeasures(GameWorld world)
            => FireWeapons<CountermeasureLauncher>(world, Vector3.Zero);

        public void FireMines(GameWorld world)
            => FireWeapons<MineDropper>(world, Vector3.Zero);

        public void FireGuns(GameWorld world)
            => FireWeapons<Gun>(world, AimPoint);

        private void FireWeapons<T>(GameWorld world, Vector3 point) where T : AbstractWeapon
        {
            if (!CanFireWeapons(world)) return;

            foreach (var wp in weapons.OfType<T>())
            {
                wp.Fire(point, world);
            }
        }

        public void FireAll(GameWorld world)
        {
            if (!CanFireWeapons(world)) return;

            foreach (var wp in weapons)
            {
                if (IsWeaponEnabled(wp))
                    wp.Fire(AimPoint, world);
            }
        }

        public IEnumerable<UiEquippedWeapon> GetUiElements()
        {
            return from wp in weapons
                select new UiEquippedWeapon(IsWeaponEnabled(wp), wp.Equipment.IdsName,
                    wp is AbstractLauncher { UsesAmmo: true } launcher ? launcher.AmmoCount : -1);
        }
    }
}
