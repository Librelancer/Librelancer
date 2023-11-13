// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.GameData.Items;
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

        public void Damage(float hullDamage, float energyDamage)
        {
            if (energyDamage <= 0) energyDamage = hullDamage / 2.0f;

            var shield = Parent.GetFirstChildComponent<SShieldComponent>();

            if (shield == null || !shield.Damage(energyDamage))
            {
                if (InfiniteHealth) return;
                CurrentHealth -= hullDamage;
                if (Parent.TryGetComponent<SNPCComponent>(out var n))
                    n.TakingDamage(hullDamage);
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
                        fuseRunner?.RunAtHealth(0);
                        if(fuseRunner == null || !fuseRunner.RunningDeathFuse)
                        {
                            FLLog.Debug("World", $"No death fuse, killing {Parent}");
                            if (Parent.TryGetComponent<SNPCComponent>(out var npc))
                                npc.Killed();
                            if (Parent.TryGetComponent<SPlayerComponent>(out var player))
                                player.Killed();
                        }
                    }
                }
            }
        }
    }
}
