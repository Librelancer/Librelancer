// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Schema.Ships;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using LibreLancer.World.Components;

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

            var shield = Parent.GetFirstChildComponent<SShieldComponent>();
            if (shield == null)
            {
                return;
            }

            if (shield.Equip.Def.MaxCapacity - shield.Health < 100)
            {
                return;
            }

            var amountToHeal = (shield.Equip.Def.MaxCapacity - shield.Health);
            var max = (int)Math.Ceiling(amountToHeal / first.Def.Hitpoints);
            var healamount = cargo.TryConsume(first, max);
            shield.Health += healamount * first.Def.Hitpoints;
            if (shield.Health > shield.Equip.Def.MaxCapacity)
            {
                shield.Health = shield.Equip.Def.MaxCapacity;
            }
        }

        // Make internal when possible
        public void HandleChildHullDamage(float hullDamage, GameObject? attacker, GameObject? child)
        {
            if (child == null)
                return;
            if (child.TryGetComponent<CargoPodComponent>(out _) && //cargo pods only for now
                child.TryGetComponent<SHealthComponent>(out var childHealth))
            {
                childHealth.HandleHullDamage(hullDamage, attacker, null);
                if(childHealth.CurrentHealth < childHealth.MaxHealth && child.Attachment != null)
                   EquipmentHealths[child.Attachment] = childHealth.CurrentHealth / childHealth.MaxHealth;
                if (Parent.TryGetComponent<SSolarComponent>(out var solar))
                    solar.SendPartsUpdate = true;
            }
        }

        private void HandleHullDamage(float hullDamage, GameObject? attacker, GameObject? child)
        {
            HandleChildHullDamage(hullDamage, attacker, child);

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

        public void DamageExplosion(float hullDamage, float energyDamage, GameObject? attacker, Vector3 origin, float radius)
        {
            if (energyDamage <= 0)
            {
                energyDamage = hullDamage / 2.0f;
            }

            var shield = Parent.GetFirstChildComponent<SShieldComponent>();

            if (shield is not null && shield.Damage(energyDamage))
            {
                return;
            }

            HandleHullDamage(hullDamage, attacker, null);
            var radiusSquared = radius * radius;
            foreach (var child in Parent.Children)
            {
                if (Vector3.DistanceSquared(child.WorldTransform.Position, origin) > radiusSquared)
                {
                    continue;
                }
                HandleChildHullDamage(hullDamage, attacker, child);
            }
        }

        public RigidModelPart? Damage(float hullDamage, float energyDamage, GameObject? attacker, object? hitObject)
        {
            if (energyDamage <= 0)
            {
                energyDamage = hullDamage / 2.0f;
            }

            var shield = Parent.GetFirstChildComponent<SShieldComponent>();

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
                    HandleHullDamage(hullDamage, attacker, null);
                }
                else if (Parent.TryGetComponent<SNPCComponent>(out var npc))
                {
                    npc.TakingDamage(hullDamage);
                }

                return destroyed ? modelPart : null;
            }

            HandleHullDamage(hullDamage, attacker, hitObject as GameObject);
            return null;
        }

        public void DamageZone(float damage)
        {
            if (damage <= 0)
                return;

            // Environmental damage bypasses shields and affects mounted equipment,
            // but Freelancer damage zones do not damage weapons.
            HandleHullDamage(damage, null, null);
            foreach (var child in Parent.Children)
            {
                if (!child.TryGetComponent<EquipmentComponent>(out var equipment) ||
                    equipment.Equipment is GunEquipment or MissileLauncherEquipment or MineDropperEquipment ||
                    !child.TryGetComponent<SHealthComponent>(out var equipmentHealth))
                    continue;

                equipmentHealth.HandleHullDamage(damage, null, null);
                if (child.Attachment is { } hardpoint)
                    EquipmentHealths[hardpoint] = equipmentHealth.CurrentHealth / equipmentHealth.MaxHealth;
            }
        }
    }
}
