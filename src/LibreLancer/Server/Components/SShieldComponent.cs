// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.GameData.Items;
using LibreLancer.World;

namespace LibreLancer.Server.Components
{
    public class SShieldComponent : GameComponent
    {
        public float Health { get; set; }

        public ShieldEquipment Equip;
        
        public double OfflineTimer { get; set; }

        private float MinHealth => Equip.Def.OfflineThreshold * Equip.Def.MaxCapacity;
        
        public SShieldComponent(ShieldEquipment equip, GameObject parent) : base(parent)
        {
            this.Equip = equip;
            this.Health = Equip.Def.MaxCapacity;
        }

        public override void Update(double time)
        {
            if (OfflineTimer > 0)
            {
                OfflineTimer -= time;
                if (OfflineTimer <= 0)
                {
                    OfflineTimer = 0;
                    Health = MinHealth;
                }
            } 
            else
            {
                Health += (float)(time * Equip.Def.RegenerationRate);
                if (Health > Equip.Def.MaxCapacity) Health = Equip.Def.MaxCapacity;
            }
        }


        public bool Damage(float incomingDamage)
        {
            if (Health > 0)
            {
                Health -= incomingDamage;
                if (Health <= 0)
                {
                    Health = 0;
                    OfflineTimer = Equip.Def.OfflineRebuildTime;
                }
                return true;
            }
            return false;
        }
    }
}