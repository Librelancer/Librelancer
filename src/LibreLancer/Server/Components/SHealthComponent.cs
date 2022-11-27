// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Linq;
using LibreLancer.World;

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
        
        public void Damage(float hullDamage, float energyDamage)
        {
            if (energyDamage <= 0) energyDamage = hullDamage / 2.0f;
            
            var shield = Parent.GetChildComponents<SShieldComponent>().FirstOrDefault();
            if (shield == null || !shield.Damage(energyDamage))
            {
                if (InfiniteHealth) return;
                CurrentHealth -= hullDamage;
                if (Invulnerable && CurrentHealth < (MaxHealth * 0.09f)) {
                    CurrentHealth = MaxHealth * 0.09f;
                }
                if (CurrentHealth <= 0) {
                    CurrentHealth = 0;
                    if (!isKilled)
                    {
                        isKilled = true;
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