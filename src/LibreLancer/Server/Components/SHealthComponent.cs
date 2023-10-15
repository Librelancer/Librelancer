// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Linq;
using LibreLancer.GameData;
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
