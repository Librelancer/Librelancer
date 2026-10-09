// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.World;
using LibreLancer.World.Components;
using LibreLancer.World.Equipments;

namespace LibreLancer.Server.Components
{
    public class SHealthComponent : GameComponent
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }

        public bool Invulnerable { get; set; }

        public bool InfiniteHealth { get; set; }

        public SHealthComponent(GameObject parent) : base(parent)
        {
        }

        private bool isKilled = false;

        public Action<GameObject, GameObject>? ProjectileHitHook;
        public Action<GameObject?>? KilledHook;

        // Optimisation for shipping over the network
        public Dictionary<Hardpoint, float> EquipmentHealths = new();


        public void OnProjectileHit(GameObject attacker)
        {
            ProjectileHitHook?.Invoke(Parent, attacker);
        }


        public void UseRepairKits()
        {
            if (!Parent.TryGetComponent<AbstractCargoComponent>(out var cargo))
            {
                return;
            }

            var first = cargo.FirstOf<RepairKitEquipment>();
            if (first == null)
            {
                return;
            }

            if (MaxHealth - CurrentHealth < 100)
            {
                return;
            }

            var amountToHeal = (MaxHealth - CurrentHealth);
            var max = (int)Math.Ceiling(amountToHeal / first.Def.Hitpoints);
            var healamount = cargo.TryConsume(first, max);
            CurrentHealth += healamount * first.Def.Hitpoints;
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }
        }

        public void UseShieldBatteries()
        {
            if (!Parent.TryGetComponent<AbstractCargoComponent>(out var cargo))
            {
                return;
            }

            var first = cargo.FirstOf<ShieldBatteryEquipment>();
            if (first == null)
            {
                return;
            }

            var shield = Parent.CoreEquipment.Shield;
            if (shield == null)
            {
                return;
            }

            if (shield.Equipment.Def.MaxCapacity - shield.ShieldHealth < 100)
            {
                return;
            }

            var amountToHeal = (shield.Equipment.Def.MaxCapacity - shield.ShieldHealth);
            var max = (int)Math.Ceiling(amountToHeal / first.Def.Hitpoints);
            var healamount = cargo.TryConsume(first, max);
            shield.ShieldHealth += healamount * first.Def.Hitpoints;
            if (shield.ShieldHealth > shield.Equipment.Def.MaxCapacity)
            {
                shield.ShieldHealth = shield.Equipment.Def.MaxCapacity;
            }
        }

        // Make internal when possible
        public void HandleChildHullDamage(float hullDamage, GameObject? attacker, EquipmentObject? child, GameWorld world)
        {
            if (child == null)
                return;
            if (child is CargoPod cargoPod)
            {
                if (cargoPod.HandleHullDamage(hullDamage))
                {
                    DestroyChild(cargoPod, world);
                }
                else
                {
                    if(cargoPod.Health < cargoPod.MaxHealth && child.Attachment != null)
                        EquipmentHealths[child.Attachment] = cargoPod.Health / cargoPod.MaxHealth;
                    if (Parent.TryGetComponent<SSolarComponent>(out var solar))
                        solar.SendPartsUpdate = true;
                }
            }
        }

        void DestroyChild(EquipmentObject child, GameWorld world)
        {
            Parent.RemoveChild(child);
        }

        private void HandleHullDamage(float hullDamage, GameObject? attacker, EquipmentObject? child, GameWorld world)
        {
            HandleChildHullDamage(hullDamage, attacker, child, world);

            if (InfiniteHealth)
            {
                return;
            }

            CurrentHealth -= hullDamage;
            if (Parent.TryGetComponent<SNPCComponent>(out var npc))
            {
                npc.TakingDamage(hullDamage);
            }

            if (Invulnerable && CurrentHealth < (MaxHealth * 0.09f))
            {
                CurrentHealth = MaxHealth * 0.09f;
            }

            var fuseRunner = Parent.GetComponent<SFuseRunnerComponent>();
            if (!isKilled && CurrentHealth > 0)
            {
                fuseRunner?.RunAtHealth(CurrentHealth);
            }

            if (!(CurrentHealth <= 0))
            {
                return;
            }

            CurrentHealth = 0;

            if (isKilled)
            {
                return;
            }

            isKilled = true;

            // If the attacker is a player, and the thing being destroyed is an NPC, increment stats
            if (attacker is not null && npc is not null &&
                attacker.TryGetComponent<SPlayerComponent>(out var attackingPlayer))
            {
                var ship = Parent.GetComponent<ShipPhysicsComponent>()!.Ship;
                attackingPlayer.Player.ShipKilledByPlayer(ship);
            }

            KilledHook?.Invoke(attacker);

            fuseRunner?.RunAtHealth(0);

            if (fuseRunner is { RunningDeathFuse: true })
            {
                return;
            }

            FLLog.Debug("World", $"No death fuse, killing {Parent}");
            if (Parent.TryGetComponent<SDestroyableComponent>(out var dst))
            {
                dst.Destroy(true);
            }
        }

        public void DamageExplosion(float hullDamage, float energyDamage, GameObject? attacker, Vector3 origin, float radius, GameWorld world)
        {
            if (energyDamage <= 0)
            {
                energyDamage = hullDamage / 2.0f;
            }

            var shield = Parent.CoreEquipment.Shield;

            if (shield is not null && shield.Damage(energyDamage))
            {
                return;
            }

            HandleHullDamage(hullDamage, attacker, null, world);
            var radiusSquared = radius * radius;
            foreach (var child in Parent.AllEquipment)
            {
                if (Vector3.DistanceSquared(child.GetTransform().Position, origin) > radiusSquared)
                {
                    continue;
                }

                HandleChildHullDamage(hullDamage, attacker, child, world);
            }
        }

        public RigidModelPart? Damage(float hullDamage, float energyDamage, GameObject? attacker, object? hitObject, GameWorld world)
        {
            if (energyDamage <= 0)
            {
                energyDamage = hullDamage / 2.0f;
            }

            var shield = Parent.CoreEquipment.Shield;

            if (shield is not null && shield.Damage(energyDamage))
            {
                return null;
            }

            var model = Parent.Model;
            if (hitObject is RigidModelPart modelPart &&
                model?.TryGetCollisionGroup(modelPart, out var collisionGroup) == true)
            {
                if (InfiniteHealth)
                {
                    return null;
                }

                var destroyed = model.DamagePart(collisionGroup, hullDamage, Invulnerable);
                if (Parent.TryGetComponent<SSolarComponent>(out var solar))
                {
                    solar.SendPartsUpdate = true;
                }

                var fuseRunner = Parent.GetComponent<SFuseRunnerComponent>();
                if (fuseRunner != null)
                {
                    foreach (var fuse in collisionGroup.Definition.Fuses)
                    {
                        if (fuse.Fuse != null &&
                            collisionGroup.CurrentHealth < fuse.Threshold &&
                            collisionGroup.RunningFuses.Add(fuse.Fuse))
                        {
                            fuseRunner.Run(fuse.Fuse);
                        }
                    }
                }

                if (collisionGroup.Definition.RootHealthProxy)
                {
                    HandleHullDamage(hullDamage, attacker, null, world);
                }
                else if (Parent.TryGetComponent<SNPCComponent>(out var npc))
                {
                    npc.TakingDamage(hullDamage);
                }

                return destroyed ? modelPart : null;
            }

            HandleHullDamage(hullDamage, attacker, hitObject as EquipmentObject, world);
            return null;
        }

        public void DamageZone(float damage, GameWorld world)
        {
            if (damage <= 0)
                return;

            // Environmental damage bypasses shields and affects mounted equipment,
            // but Freelancer damage zones do not damage weapons.
            HandleHullDamage(damage, null, null, world);
            List<EquipmentObject> toDestroy = [];
            foreach (var child in Parent.AllEquipment)
            {
                if (child is DamageCap ||
                    child.Equipment is GunEquipment or MissileLauncherEquipment or MineDropperEquipment ||
                    child.Invincible)
                    continue;

                if (child.HandleHullDamage(damage))
                {
                    toDestroy.Add(child);
                }
                else if (child.Attachment is { } hardpoint)
                {
                    EquipmentHealths[hardpoint] = child.Health / child.MaxHealth;
                }
            }

            foreach (var c in toDestroy)
            {
                DestroyChild(c, world);
            }
        }
    }
}
