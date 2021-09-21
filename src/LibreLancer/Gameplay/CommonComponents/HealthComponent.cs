// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer
{
    public class HealthComponent : GameComponent
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }

        public HealthComponent(GameObject parent) : base(parent)
        {
        }

        public void Damage(float amount)
        {
            CurrentHealth -= amount;
            if (CurrentHealth <= 0) {
                CurrentHealth = 0;
            }
        }
    }
}