// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.GameData.Items;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components
{
    public class SShieldComponent : GameComponent
    {
        public float Health
        {
            get => _health < MinHealth ? 0 : _health;
            set => _health = value;
        }

        private float _health;

        public ShieldEquipment Equip;

        private float MinHealth => Equip.Def.OfflineThreshold * Equip.Def.MaxCapacity;
        private double suppressedUntil;
        private float suppressedRestoreHealth;

        public SShieldComponent(ShieldEquipment equip, GameObject parent) : base(parent)
        {
            this.Equip = equip;
            this.Health = Equip.Def.MaxCapacity;
        }

        public void Suppress(double duration, GameWorld world)
        {
            var totalTime = world.Server?.Server.TotalTime ?? 0;
            suppressedUntil = Math.Max(suppressedUntil, totalTime + duration);
            suppressedRestoreHealth = Math.Max(suppressedRestoreHealth, _health);
            _health = 0;
        }

        private bool shieldHpActive = false;

        public override void Update(double time, GameWorld world)
        {
            var totalTime = world.Server?.Server.TotalTime ?? 0;
            if (suppressedUntil > totalTime)
            {
                _health = 0;
            }
            else if (suppressedUntil > 0)
            {
                _health = Math.Max(suppressedRestoreHealth, MinHealth);
                suppressedRestoreHealth = 0;
                suppressedUntil = 0;
            }

            if (_health < MinHealth)
            {
                var regenRate = MinHealth / Equip.Def.OfflineRebuildTime;
                _health += (float) (time * regenRate);
                if (_health > MinHealth)
                    _health = MinHealth;
            }
            else
            {
                _health += (float)(time * Equip.Def.RegenerationRate);
                if (_health > Equip.Def.MaxCapacity) _health = Equip.Def.MaxCapacity;
            }

            if (Health >= MinHealth && !shieldHpActive)
            {
                shieldHpActive = true;
                if (Parent.Parent!.TryGetComponent<ShipComponent>(out var ship)) {
                    ship.ActivateShieldBubble(Parent.Attachment!.Name);
                }
            }
            else if (Health < MinHealth && shieldHpActive)
            {
                shieldHpActive = false;
                if (Parent.Parent!.TryGetComponent<ShipComponent>(out var ship)) {
                    ship.DeactivateShieldBubble(Parent.Attachment!.Name);
                }
            }
        }

        public bool Damage(float incomingDamage)
        {
            if (_health > MinHealth)
            {
                _health -= incomingDamage;
                if (_health <= MinHealth)
                {
                    _health = 0;
                }
                if(Parent.Parent!.TryGetComponent<SNPCComponent>(out var n))
                    n.TakingDamage(incomingDamage);
                if (Parent.Parent!.TryGetComponent<SSolarComponent>(out var s))
                    s.SendSolarUpdate = true;
                return true;
            }
            return false;
        }
    }
}
