// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

        float _health;

        public ShieldEquipment Equip;

        private float MinHealth => Equip.Def.OfflineThreshold * Equip.Def.MaxCapacity;

        public SShieldComponent(ShieldEquipment equip, GameObject parent) : base(parent)
        {
            this.Equip = equip;
            this.Health = Equip.Def.MaxCapacity;
        }

        private bool shieldHpActive = false;

        public override void Update(double time)
        {
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
                if (Parent.Parent.TryGetComponent<ShipComponent>(out var ship)) {
                    ship.ActivateShieldBubble(Parent.Attachment.Name);
                }
            }
            else if (Health < MinHealth && shieldHpActive)
            {
                shieldHpActive = false;
                if (Parent.Parent.TryGetComponent<ShipComponent>(out var ship)) {
                    ship.DeactivateShieldBubble(Parent.Attachment.Name);
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
                if(Parent.Parent.TryGetComponent<SNPCComponent>(out var n))
                    n.TakingDamage(incomingDamage);
                return true;
            }
            return false;
        }
    }
}
