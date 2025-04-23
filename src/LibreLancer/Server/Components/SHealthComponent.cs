// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ships;
using LibreLancer.GameData.Items;
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

        public SHealthComponent(GameObject parent) : base(parent) { }

        private bool isKilled = false;

        public Action<GameObject, GameObject> ProjectileHitHook;

        public void OnProjectileHit(GameObject attacker)
        {
            ProjectileHitHook?.Invoke(Parent, attacker);
        }

        public void UseRepairKits()
        {
            if (!Parent.TryGetComponent<AbstractCargoComponent>(out var cargo))
                return;
            var first = cargo.FirstOf<RepairKitEquipment>();
            if (first == null)
                return;
            if (MaxHealth - CurrentHealth < 100)
                return;
            var amountToHeal = (MaxHealth - CurrentHealth);
            var max = (int) Math.Ceiling(amountToHeal / first.Def.Hitpoints);
            var healamount = cargo.TryConsume(first, max);
            CurrentHealth += healamount * first.Def.Hitpoints;
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
        }

        public void UseShieldBatteries()
        {
            if (!Parent.TryGetComponent<AbstractCargoComponent>(out var cargo))
                return;
            var first = cargo.FirstOf<ShieldBatteryEquipment>();
            if (first == null)
                return;
            var shield = Parent.GetFirstChildComponent<SShieldComponent>();
            if (shield == null)
                return;
            if (shield.Equip.Def.MaxCapacity - shield.Health < 100)
                return;
            var amountToHeal = (shield.Equip.Def.MaxCapacity - shield.Health);
            var max = (int) Math.Ceiling(amountToHeal / first.Def.Hitpoints);
            var healamount = cargo.TryConsume(first, max);
            shield.Health += healamount * first.Def.Hitpoints;
            if (shield.Health > shield.Equip.Def.MaxCapacity)
                shield.Health = shield.Equip.Def.MaxCapacity;
        }

        public void Damage(float hullDamage, float energyDamage, GameObject attacker)
        {
            if (energyDamage <= 0) energyDamage = hullDamage / 2.0f;

            var shield = Parent.GetFirstChildComponent<SShieldComponent>();

            if (shield == null || !shield.Damage(energyDamage))
            {
                if (InfiniteHealth) return;
                CurrentHealth -= hullDamage;
                if (Parent.TryGetComponent<SNPCComponent>(out var npc))
                {
                    npc.TakingDamage(hullDamage);
                }

                if (Invulnerable && CurrentHealth < (MaxHealth * 0.09f)) {
                    CurrentHealth = MaxHealth * 0.09f;
                }
                var fuseRunner = Parent.GetComponent<SFuseRunnerComponent>();
                if (!isKilled && CurrentHealth > 0)
                    fuseRunner?.RunAtHealth(CurrentHealth);
                if (CurrentHealth <= 0) {
                    CurrentHealth = 0;
                    if (!isKilled)
                    {
                        isKilled = true;

                        // If the attacker is a player, and the thing being destroyed is an NPC, increment stats
                        if (attacker is not null && npc is not null && attacker.TryGetComponent<SPlayerComponent>(out var attackingPlayer))
                        {
                            var ship = Parent.GetComponent<ShipPhysicsComponent>().Ship;

                            NetPlayerStatistics statistics = attackingPlayer.Player.Character.Statistics;
                            switch (ship.ShipType)
                            {
                                case ShipType.Fighter:
                                    statistics.FightersKilled++;
                                    break;
                                case ShipType.Freighter:
                                    statistics.FreightersKilled++;
                                    break;
                                case ShipType.Capital:
                                    statistics.BattleshipsKilled++;
                                    break;
                                case ShipType.Transport:
                                    statistics.TransportsKilled++;
                                    break;
                            }

                            attackingPlayer.Player.UpdateStatistics(statistics);
                        }

                        fuseRunner?.RunAtHealth(0);
                        if(fuseRunner == null || !fuseRunner.RunningDeathFuse)
                        {
                            FLLog.Debug("World", $"No death fuse, killing {Parent}");
                            if (Parent.TryGetComponent<SDestroyableComponent>(out var dst))
                            {
                                dst.Destroy(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
